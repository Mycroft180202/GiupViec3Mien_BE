using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Messaging;
using GiupViec3Mien.Services.Elastic;
using Elastic.Clients.Elasticsearch;
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
    private readonly GiupViec3Mien.Services.Elastic.IJobSearchService _jobSearchService;

    public JobService(IJobRepository jobRepository, 
                      IJobApplicationRepository applicationRepository, 
                      IFileStorageService fileStorageService,
                      IPublishEndpoint publishEndpoint,
                      IUserRepository userRepository,
                      IBackgroundJobClient backgroundJobClient,
                      GiupViec3Mien.Services.Elastic.IJobSearchService jobSearchService)
    {
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _fileStorageService = fileStorageService;
        _publishEndpoint = publishEndpoint;
        _userRepository = userRepository;
        _backgroundJobClient = backgroundJobClient;
        _jobSearchService = jobSearchService;
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
            RequiredSkills = request.RequiredSkills != null ? JsonSerializer.Serialize(request.RequiredSkills) : null,
            PostType = request.PostType,
            TimingType = request.TimingType,
            ServiceCategory = request.ServiceCategory,
            WorkingTimeDescription = request.WorkingTimeDescription,
            PreferredGender = request.PreferredGender,
            TargetAgeRange = request.TargetAgeRange
        };

        await _jobRepository.AddAsync(job);
        await _jobRepository.SaveChangesAsync();

        // 1. Analytics
        await _publishEndpoint.Publish(new AnalyticsEvent("JobCreated", employerId, $"Title: {job.Title}"));

        // 2. Background Matching
        await _publishEndpoint.Publish(new JobPostedEvent(
            job.Id, job.Title, job.Latitude, job.Longitude, job.RequiredSkills));

        // 3. Decoupled Email
        var employer = await _userRepository.GetByIdAsync(employerId);
        if (employer != null && !string.IsNullOrEmpty(employer.Email))
        {
            await _publishEndpoint.Publish(new SendEmailMessage(employer.Email, $"Job Posted: {job.Title}", $"Your job {job.Title} is now live."));
        }

        // 4. Direct Elasticsearch Sync (Atomic update)
        var jobDoc = MapToDocument(job);
        await _jobSearchService.BulkIndexAsync(new[] { jobDoc });

        return MapToResponse(job, true);
    }

    public async Task<JobResponse?> UpdateJobAsync(Guid userId, Guid jobId, CreateJobRequest request)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null || job.EmployerId != userId) return null;

        job.Title = request.Title;
        job.Description = request.Description;
        job.Location = request.Location;
        job.Latitude = request.Latitude;
        job.Longitude = request.Longitude;
        job.Price = request.Price;
        job.RequiredSkills = request.RequiredSkills != null ? JsonSerializer.Serialize(request.RequiredSkills) : null;
        job.PostType = request.PostType;
        job.TimingType = request.TimingType;
        job.ServiceCategory = request.ServiceCategory;
        job.WorkingTimeDescription = request.WorkingTimeDescription;
        job.PreferredGender = request.PreferredGender;
        job.TargetAgeRange = request.TargetAgeRange;
        job.UpdatedAt = DateTime.UtcNow;

        await _jobRepository.SaveChangesAsync();

        // Direct Elasticsearch Sync
        var jobDoc = MapToDocument(job);
        await _jobSearchService.BulkIndexAsync(new[] { jobDoc });

        return MapToResponse(job, true);
    }

    public async Task<bool> DeleteJobAsync(Guid userId, Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null || job.EmployerId != userId) return false;

        await _jobRepository.DeleteAsync(job);
        await _jobRepository.SaveChangesAsync();

        // Direct Elasticsearch Sync
        await _jobSearchService.DeleteAsync(jobId);

        return true;
    }

    public async Task<IEnumerable<JobResponse>> GetAvailableJobsAsync()
    {
        var jobs = await _jobRepository.GetActiveJobsAsync();
        return jobs.Select(j => MapToResponse(j, false));
    }

    public async Task<IEnumerable<JobResponse>> GetWorkerAdsAsync()
    {
        var ads = await _jobRepository.GetJobsByPostTypeAsync(Domain.Enums.PostType.Seeking);
        return ads.Select(j => MapToResponse(j, false));
    }

    public async Task<JobResponse?> GetJobDetailsAsync(Guid jobId, Guid requesterId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;

        var requester = requesterId != Guid.Empty ? await _userRepository.GetByIdAsync(requesterId) : null;
        bool hasFullAccess = job.EmployerId == requesterId || 
                            job.AssignedWorkerId == requesterId ||
                            (requester != null && (requester.Role == Domain.Enums.Role.Admin || 
                            (requester.HasPremiumAccess && requester.PremiumExpiry >= DateTime.UtcNow)));
        
        return MapToResponse(job, hasFullAccess);
    }

    public async Task<IEnumerable<JobResponse>> GetJobsByEmployerAsync(Guid employerId)
    {
        var jobs = await _jobRepository.GetJobsByEmployerAsync(employerId);
        return jobs.Select(j => MapToResponse(j, true));
    }

    public async Task<IEnumerable<JobResponse>> GetJobsByWorkerAsync(Guid workerId)
    {
        var jobs = await _jobRepository.GetByAssignedWorkerIdAsync(workerId);
        return jobs.Select(j => MapToResponse(j, j.AssignedWorkerId == workerId));
    }

    public async Task<IEnumerable<JobResponse>> GetMyAdsAsync(Guid userId)
    {
        var jobs = await _jobRepository.GetJobsByEmployerAsync(userId);
        return jobs.Where(j => j.PostType == Domain.Enums.PostType.Seeking).Select(j => MapToResponse(j, true));
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
            if (!allowedExtensions.Contains(extension)) throw new Exception("File type not allowed.");

            using var ms = new MemoryStream();
            await request.Cv.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            
            _backgroundJobClient.Enqueue<BackgroundJobs.ProcessCVJob>(j => 
                j.ExecuteAsync(userId, jobId, fileBytes, request.Cv.FileName));
            
            cvUrl = "[Processing...]"; 
        }

        var bidPrice = request.BidPrice > 0 ? request.BidPrice : job.Price;
        await _publishEndpoint.Publish(new JobApplicationTask(userId, jobId, request.Message ?? "", bidPrice, cvUrl));
        await _publishEndpoint.Publish(new AnalyticsEvent("JobApplied", userId, $"JobId: {jobId}"));

        return new JobApplicationResponse { JobId = jobId, ApplicantId = userId, Message = request.Message, BidPrice = bidPrice, CvUrl = cvUrl, AppliedAt = DateTime.UtcNow, IsAccepted = false };
    }

    public async Task<IEnumerable<JobApplicationResponse>> GetJobApplicationsAsync(Guid jobId, Guid requesterId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester == null) throw new Exception("Requester not found.");

        if (job.EmployerId != requesterId && requester.Role != Domain.Enums.Role.Admin) throw new Exception("Unauthorized.");

        var applications = await _applicationRepository.GetByJobIdAsync(jobId);
        bool hasFullContactAccess = requester.Role == Domain.Enums.Role.Admin || (requester.HasPremiumAccess && requester.PremiumExpiry >= DateTime.UtcNow);

        return applications.Select(a => MapToApplicationResponse(a, hasFullContactAccess || a.ApplicantId == requesterId));
    }

    private JobApplicationResponse MapToApplicationResponse(Domain.Entities.JobApplication a, bool fullAccess)
    {
        return new JobApplicationResponse
        {
            Id = a.Id, JobId = a.JobId, JobTitle = a.Job?.Title ?? "Unknown Job", JobPrice = a.Job?.Price ?? 0,
            ApplicantId = a.ApplicantId, ApplicantName = a.Applicant?.FullName ?? "Unknown",
            ApplicantPhone = fullAccess ? a.Applicant?.Phone : MaskContact(a.Applicant?.Phone),
            ApplicantEmail = fullAccess ? a.Applicant?.Email : MaskContact(a.Applicant?.Email),
            Message = a.Message, BidPrice = a.BidPrice, CvUrl = a.CvUrl, AppliedAt = a.AppliedAt, IsAccepted = a.IsAccepted
        };
    }

    private string? MaskContact(string? contact)
    {
        if (string.IsNullOrEmpty(contact)) return contact;
        if (contact.Contains("@"))
        {
            var parts = contact.Split('@');
            return parts[0].Length > 2 ? parts[0][..2] + "***@" + parts[1] : "***@" + parts[1];
        }
        return contact.Length > 4 ? contact[..3] + "****" + contact[^3..] : "****";
    }

    public async Task<IEnumerable<JobApplicationResponse>> GetMyApplicationsAsync(Guid userId)
    {
        var applications = await _applicationRepository.GetByApplicantIdAsync(userId);
        return applications.Select(a => MapToApplicationResponse(a, true));
    }

    public async Task<IEnumerable<JobApplicationResponse>> GetReceivedApplicationsAsync(Guid employerId)
    {
        var applications = await _applicationRepository.GetByEmployerIdAsync(employerId);
        return applications.Select(a => MapToApplicationResponse(a, true));
    }

    public async Task<JobApplicationResponse?> GetMyApplicationForJobAsync(Guid userId, Guid jobId)
    {
        var application = await _applicationRepository.GetByApplicantAndJobAsync(userId, jobId);
        if (application == null) return null;
        return MapToApplicationResponse(application, true);
    }

    public async Task<JobApplicationResponse?> AcceptApplicationAsync(Guid employerId, Guid applicationId)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null) return null;

        var job = await _jobRepository.GetByIdAsync(application.JobId);
        if (job == null || job.EmployerId != employerId) return null;

        application.IsAccepted = true;
        job.Status = Domain.Enums.JobStatus.InProgress;
        job.AssignedWorkerId = application.ApplicantId;
        job.UpdatedAt = DateTime.UtcNow;

        await _applicationRepository.SaveChangesAsync();
        await _jobRepository.SaveChangesAsync();

        // Direct Elasticsearch Sync
        var jobDoc = MapToDocument(job);
        await _jobSearchService.BulkIndexAsync(new[] { jobDoc });

        await _publishEndpoint.Publish(new AnalyticsEvent("ApplicationAccepted", employerId, $"AppId: {applicationId}"));

        if (application.Applicant != null && !string.IsNullOrEmpty(application.Applicant.Email))
        {
            string subject = $"Congratulations! Your application for '{job.Title}' has been accepted";
            string body = $"<p>Hi <strong>{application.Applicant.FullName}</strong>,</p><p>Your application for <strong>\"{job.Title}\"</strong> has been accepted.</p>";
            await _publishEndpoint.Publish(new SendEmailMessage(application.Applicant.Email, subject, body));
        }

        return MapToApplicationResponse(application, true);
    }

    public async Task<IEnumerable<JobResponse>> GetJobsBySkillsAsync(IEnumerable<string> skills)
    {
        var jobs = await _jobRepository.GetJobsBySkillsAsync(skills);
        return jobs.Select(j => MapToResponse(j, false));
    }

    private JobResponse MapToResponse(Domain.Entities.Job job, bool fullAccess)
    {
        return new JobResponse
        {
            Id = job.Id, EmployerId = job.EmployerId, EmployerName = job.Employer?.FullName ?? "Unknown", 
            EmployerAvatarUrl = job.Employer?.AvatarUrl, 
            EmployerPhone = fullAccess ? job.Employer?.Phone : MaskContact(job.Employer?.Phone),
            EmployerEmail = fullAccess ? job.Employer?.Email : MaskContact(job.Employer?.Email),
            CompanyHotline = "1900-xxxx (Giúp Việc 3 Miền Support)",
            Title = job.Title, Description = job.Description, Location = job.Location,
            Price = job.Price, Latitude = job.Latitude, Longitude = job.Longitude,
            RequiredSkills = string.IsNullOrEmpty(job.RequiredSkills) ? null : JsonSerializer.Deserialize<List<string>>(job.RequiredSkills),
            Status = job.Status, PostType = job.PostType, TimingType = job.TimingType, ServiceCategory = job.ServiceCategory,
            WorkingTimeDescription = job.WorkingTimeDescription, PreferredGender = job.PreferredGender, 
            TargetAgeRange = job.TargetAgeRange, ApplicantCount = job.Applications?.Count ?? 0,
            AssignedWorkerId = job.AssignedWorkerId, AssignedWorkerName = job.AssignedWorker?.FullName, CreatedAt = job.CreatedAt
        };
    }

    public async Task<IEnumerable<JobResponse>> GetAllJobsAsync()
    {
        var jobs = await _jobRepository.GetAllAsync();
        return jobs.Select(j => MapToResponse(j, true));
    }

    public async Task<bool> DeleteViolatingJobAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return false;

        var employerEmail = job.Employer?.Email;
        var jobTitle = job.Title;

        await _jobRepository.DeleteAsync(job);
        await _jobRepository.SaveChangesAsync();

        // Direct Elasticsearch Sync
        await _jobSearchService.DeleteAsync(jobId);
        await _publishEndpoint.Publish(new AnalyticsEvent("JobDeletedByAdmin", Guid.Empty, $"JobId: {jobId}"));

        if (!string.IsNullOrEmpty(employerEmail))
        {
            await _publishEndpoint.Publish(new SendEmailMessage(employerEmail, "Post Removed", $"\"{jobTitle}\" removed."));
        }
        return true;
    }

    public async Task<IEnumerable<JobResponse>> SearchJobsAsync(JobSearchFilters filters)
    {
        var jobDocs = await _jobSearchService.SearchAsync(filters);
        return jobDocs.Select(MapFromDocument);
    }

    private JobResponse MapFromDocument(Elastic.JobDocument doc)
    {
        return new JobResponse
        {
            Id = Guid.Parse(doc.Id), EmployerId = doc.EmployerId, EmployerName = doc.EmployerName, EmployerAvatarUrl = doc.EmployerAvatarUrl,
            CompanyHotline = "1900-xxxx (Giúp Việc 3 Miền Support)",
            Title = doc.Title, Description = doc.Description, Location = doc.Location, Price = doc.Price,
            Latitude = doc.Coordinates?.Lat ?? 0, Longitude = doc.Coordinates?.Lon ?? 0,
            RequiredSkills = doc.RequiredSkills ?? new List<string>(),
            Status = !string.IsNullOrEmpty(doc.Status) ? Enum.Parse<Domain.Enums.JobStatus>(doc.Status, true) : Domain.Enums.JobStatus.Open,
            PostType = !string.IsNullOrEmpty(doc.PostType) ? Enum.Parse<Domain.Enums.PostType>(doc.PostType, true) : Domain.Enums.PostType.Hiring,
            TimingType = !string.IsNullOrEmpty(doc.TimingType) ? Enum.Parse<Domain.Enums.JobTimingType>(doc.TimingType, true) : Domain.Enums.JobTimingType.PartTime,
            ServiceCategory = !string.IsNullOrEmpty(doc.Category) ? Enum.Parse<Domain.Enums.ServiceCategory>(doc.Category, true) : Domain.Enums.ServiceCategory.Other,
            ApplicantCount = doc.ApplicantCount, CreatedAt = doc.CreatedAt

        };
    }

    public async Task<int> GetTotalApplicationCountAsync() => await _applicationRepository.CountAsync();

    public async Task<JobResponse?> UpdateJobStatusAsync(Guid jobId, Domain.Enums.JobStatus newStatus)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;
        job.Status = newStatus;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepository.SaveChangesAsync();

        // Direct Elasticsearch Sync
        var jobDoc = MapToDocument(job);
        await _jobSearchService.BulkIndexAsync(new[] { jobDoc });

        return MapToResponse(job, true);
    }

    public async Task<IDictionary<Domain.Enums.JobStatus, int>> GetJobStatusCountsAsync()
    {
        var jobs = await _jobRepository.GetAllAsync();
        return jobs.GroupBy(j => j.Status).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<int> ReindexAllJobsAsync()
    {
        await _jobSearchService.InitializeIndexAsync();
        var jobs = await _jobRepository.GetAllAsync();
        var documents = jobs.Select(MapToDocument).ToList();
        if (documents.Any()) await _jobSearchService.BulkIndexAsync(documents);
        return documents.Count;
    }

    private Elastic.JobDocument MapToDocument(Domain.Entities.Job job)
    {
        return new Elastic.JobDocument
        {
            Id = job.Id.ToString(), Title = job.Title, Description = job.Description, Location = job.Location,
            Category = job.ServiceCategory.ToString().ToLowerInvariant(), Price = job.Price,
            Coordinates = (job.Latitude != 0 || job.Longitude != 0) ? new Elastic.JobGeoPoint(job.Latitude, job.Longitude) : null,
            RequiredSkills = string.IsNullOrEmpty(job.RequiredSkills) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(job.RequiredSkills) ?? new List<string>(),
            Status = job.Status.ToString().ToLowerInvariant(), PostType = job.PostType.ToString().ToLowerInvariant(),
            TimingType = job.TimingType.ToString().ToLowerInvariant(),
            CreatedAt = job.CreatedAt, EmployerId = job.EmployerId, EmployerName = job.Employer?.FullName ?? "Unknown", EmployerAvatarUrl = job.Employer?.AvatarUrl, ApplicantCount = job.Applications?.Count ?? 0

        };
    }
}
