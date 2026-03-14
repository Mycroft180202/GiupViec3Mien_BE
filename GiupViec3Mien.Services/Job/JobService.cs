using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.FileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace GiupViec3Mien.Services.Job;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;

    public JobService(IJobRepository jobRepository, 
                      IJobApplicationRepository applicationRepository, 
                      IFileStorageService fileStorageService,
                      IEmailService emailService,
                      IUserRepository userRepository)
    {
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _fileStorageService = fileStorageService;
        _emailService = emailService;
        _userRepository = userRepository;
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
            Price = request.Price
        };

        await _jobRepository.AddAsync(job);
        await _jobRepository.SaveChangesAsync();

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
            var allowedExtensions = new[] { ".pdf", ".docx", ".txt", ".doc" };
            var extension = Path.GetExtension(request.Cv.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("File type not allowed. Please upload pdf, docx, txt, or doc.");
            }

            cvUrl = await _fileStorageService.UploadFileAsync(request.Cv);
        }

        var application = new Domain.Entities.JobApplication
        {
            JobId = jobId,
            ApplicantId = userId,
            Message = request.Message,
            BidPrice = request.BidPrice > 0 ? request.BidPrice : job.Price,
            CvUrl = cvUrl
        };

        await _applicationRepository.AddAsync(application);
        await _applicationRepository.SaveChangesAsync();

        // Notify Employer via Email
        if (job.Employer != null && !string.IsNullOrEmpty(job.Employer.Email))
        {
            try
            {
                var freelancer = await _userRepository.GetByIdAsync(userId);
                string subject = $"New application for your job: {job.Title}";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; line-height: 1.6;'>
                        <h2 style='color: #2c3e50;'>New Job Application</h2>
                        <p>Hi <strong>{job.Employer.FullName}</strong>,</p>
                        <p>A freelancer has applied for your job <strong>""{job.Title}""</strong>.</p>
                        <div style='margin: 20px 0; padding: 15px; border: 1px solid #eee; border-radius: 5px; background: #f9f9f9;'>
                            <strong>Freelancer:</strong> {freelancer?.FullName ?? "Someone"}<br/>
                            <strong>Bid Price:</strong> {application.BidPrice:N0} VND<br/>
                            <strong>Message:</strong> {application.Message}
                        </div>
                        <p>Log in to your dashboard to review the application and contact the freelancer.</p>
                        <div style='margin-top: 20px;'>
                            <a href='#' style='background: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Review Application</a>
                        </div>
                    </div>";

                await _emailService.SendEmailAsync(job.Employer.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send application notification email: {ex.Message}");
            }
        }

        return MapToApplicationResponse(application);
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

        // Notify Freelancer via Email
        if (application.Applicant != null && !string.IsNullOrEmpty(application.Applicant.Email))
        {
            try
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

                await _emailService.SendEmailAsync(application.Applicant.Email, subject, body);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the transaction
                Console.WriteLine($"Failed to send acceptance email: {ex.Message}");
            }
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
            RequiredSkills = job.RequiredSkills,
            Status = job.Status,
            ApplicantCount = job.Applications?.Count ?? 0,
            AssignedWorkerId = job.AssignedWorkerId,
            AssignedWorkerName = job.AssignedWorker?.FullName,
            CreatedAt = job.CreatedAt
        };
    }
}
