using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Hangfire;

namespace GiupViec3Mien.Services.Job;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUserRepository _userRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobService(IJobRepository jobRepository, 
                      IJobApplicationRepository applicationRepository, 
                      IFileStorageService fileStorageService,
                      IPublishEndpoint publishEndpoint,
                      IUserRepository userRepository,
                      IBackgroundJobClient backgroundJobClient)
    {
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _fileStorageService = fileStorageService;
        _publishEndpoint = publishEndpoint;
        _userRepository = userRepository;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<JobResponse> CreateJobAsync(Guid employerId, CreateJobRequest request)
    {
        var job = new Domain.Entities.Job
        {
            EmployerId = employerId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Price = request.Price,
            RequiredSkills = request.RequiredSkills != null ? JsonSerializer.Serialize(request.RequiredSkills) : null
        };

        await _jobRepository.AddAsync(job);
        await _jobRepository.SaveChangesAsync();

        // Point 4: Analytics
        await _publishEndpoint.Publish(new AnalyticsEvent("JobCreated", employerId, $"Title: {job.Title}"));

        // Point 2: Background Matching
        await _publishEndpoint.Publish(new JobPostedEvent(
            job.Id, 
            job.Title, 
            job.Latitude, 
            job.Longitude, 
            job.RequiredSkills
        ));

        // Point 1: Decoupled Email
        var employer = await _userRepository.GetByIdAsync(employerId);
        if (employer != null && !string.IsNullOrEmpty(employer.Email))
        {
            await _publishEndpoint.Publish(new SendEmailMessage(employer.Email, $"Job Posted: {job.Title}", $"Your job {job.Title} is now live."));
        }

        return MapToResponse(job);
    }

    public async Task<IEnumerable<JobResponse>> GetAvailableJobsAsync()
    {
        var jobs = await _jobRepository.GetActiveJobsAsync();
        return jobs.Select(MapToResponse);
    }

    public async Task<JobResponse?> GetJobDetailsAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;

        return MapToResponse(job);
    }

    public async Task<IEnumerable<JobResponse>> GetJobsByEmployerAsync(Guid employerId)
    {
        var jobs = await _jobRepository.GetJobsByEmployerAsync(employerId);
        return jobs.Select(MapToResponse);
    }

    public async Task<JobApplicationResponse> ApplyToJobAsync(Guid userId, Guid jobId, ApplyJobRequest request)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        var existing = await _applicationRepository.GetByApplicantAndJobAsync(userId, jobId);
        if (existing != null) throw new Exception("You have already applied to this job.");

        string? cvUrl = null;
        if (request.Cv != null && request.Cv.Length > 0)
        {
            // Simple validation remains in API
            var allowedExtensions = new[] { ".pdf", ".docx", ".txt", ".doc" };
            var extension = Path.GetExtension(request.Cv.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("File type not allowed. Please upload pdf, docx, txt, or doc.");
            }

            // Offload heavy logic: Read bytes and queue for reliable background upload
            using var ms = new MemoryStream();
            await request.Cv.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            
            _backgroundJobClient.Enqueue<BackgroundJobs.ProcessCVJob>(j => 
                j.ExecuteAsync(userId, jobId, fileBytes, request.Cv.FileName));
            
            // Note: Returning 'Pending' URL metadata as we've moved upload to background
            cvUrl = "[Processing...]"; 
        }

        // Point 5: Handling Bursts - Publish task
        var bidPrice = request.BidPrice > 0 ? request.BidPrice : job.Price;
        await _publishEndpoint.Publish(new JobApplicationTask(userId, jobId, request.Message ?? "", bidPrice, cvUrl));

        // Point 4: Analytics
        await _publishEndpoint.Publish(new AnalyticsEvent("JobApplied", userId, $"JobId: {jobId}"));

        return new JobApplicationResponse
        {
            JobId = jobId,
            ApplicantId = userId,
            Message = request.Message,
            BidPrice = bidPrice,
            CvUrl = cvUrl,
            AppliedAt = DateTime.UtcNow,
            IsAccepted = false
        };
    }

    public async Task<IEnumerable<JobApplicationResponse>> GetJobApplicationsAsync(Guid jobId)
    {
        var applications = await _applicationRepository.GetByJobIdAsync(jobId);
        return applications.Select(MapToApplicationResponse);
    }

    public async Task<IEnumerable<JobApplicationResponse>> GetMyApplicationsAsync(Guid userId)
    {
        var applications = await _applicationRepository.GetByApplicantIdAsync(userId);
        return applications.Select(MapToApplicationResponse);
    }

    public async Task<JobApplicationResponse?> GetMyApplicationForJobAsync(Guid userId, Guid jobId)
    {
        var application = await _applicationRepository.GetByApplicantAndJobAsync(userId, jobId);
        if (application == null) return null;

        return MapToApplicationResponse(application);
    }

    public async Task<JobApplicationResponse?> AcceptApplicationAsync(Guid employerId, Guid applicationId)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null) return null;

        var job = await _jobRepository.GetByIdAsync(application.JobId);
        if (job == null || job.EmployerId != employerId) return null;

        // Update application
        application.IsAccepted = true;

        // Update job
        job.Status = Domain.Enums.JobStatus.InProgress;
        job.AssignedWorkerId = application.ApplicantId;
        job.UpdatedAt = DateTime.UtcNow;

        await _applicationRepository.SaveChangesAsync();
        await _jobRepository.SaveChangesAsync();

        // Point 4: Analytics
        await _publishEndpoint.Publish(new AnalyticsEvent("ApplicationAccepted", employerId, $"AppId: {applicationId}"));

        // Notify Freelancer via Email (Point 1: Offloading)
        if (application.Applicant != null && !string.IsNullOrEmpty(application.Applicant.Email))
        {
            string subject = $"Congratulations! Your application for '{job.Title}' has been accepted";
            string body = $@"
                <div style='font-family: sans-serif; max-width: 600px; line-height: 1.6;'>
                    <h2 style='color: #2e7d32;'>Good news!</h2>
                    <p>Hi <strong>{application.Applicant.FullName}</strong>,</p>
                    <p>Your application for the job <strong>""{job.Title}""</strong> has been accepted by <strong>{job.Employer?.FullName ?? "the client"}</strong>.</p>
                    <p>The job status is now <strong>In Progress</strong>. You can now start communicating with the client directly in the app.</p>
                    <div style='margin: 20px 0; padding: 15px; border-left: 5px solid #2e7d32; background: #e8f5e9;'>
                        <strong>Job:</strong> {job.Title}<br/>
                        <strong>Client:</strong> {job.Employer?.FullName ?? "N/A"}
                    </div>
                    <p>Good luck with your new assignment!</p>
                </div>";

            await _publishEndpoint.Publish(new SendEmailMessage(application.Applicant.Email, subject, body));
        }

        return MapToApplicationResponse(application);
    }

    private JobApplicationResponse MapToApplicationResponse(Domain.Entities.JobApplication a)
    {
        return new JobApplicationResponse
        {
            Id = a.Id,
            JobId = a.JobId,
            JobTitle = a.Job?.Title ?? "Unknown Job",
            JobPrice = a.Job?.Price ?? 0,
            ApplicantId = a.ApplicantId,
            ApplicantName = a.Applicant?.FullName ?? "Unknown",
            Message = a.Message,
            BidPrice = a.BidPrice,
            CvUrl = a.CvUrl,
            AppliedAt = a.AppliedAt,
            IsAccepted = a.IsAccepted
        };
    }

    public async Task<IEnumerable<JobResponse>> GetJobsBySkillsAsync(IEnumerable<string> skills)
    {
        var jobs = await _jobRepository.GetJobsBySkillsAsync(skills);
        return jobs.Select(MapToResponse);
    }

    private JobResponse MapToResponse(Domain.Entities.Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            EmployerId = job.EmployerId,
            EmployerName = job.Employer?.FullName ?? "Unknown",
            EmployerAvatarUrl = job.Employer?.AvatarUrl,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            Price = job.Price,
            Latitude = job.Latitude,
            Longitude = job.Longitude,
            RequiredSkills = string.IsNullOrEmpty(job.RequiredSkills) 
                ? null 
                : JsonSerializer.Deserialize<List<string>>(job.RequiredSkills),
            Status = job.Status,
            ApplicantCount = job.Applications?.Count ?? 0,
            AssignedWorkerId = job.AssignedWorkerId,
            AssignedWorkerName = job.AssignedWorker?.FullName,
            CreatedAt = job.CreatedAt
        };
    }
}
