using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Messaging;
using GiupViec3Mien.Services.Notification;
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
    private readonly INotificationService _notificationService;
    private readonly IDistributedLockService _distributedLockService;

    public JobService(IJobRepository jobRepository, 
                      IJobApplicationRepository applicationRepository, 
                      IFileStorageService fileStorageService,
                      IPublishEndpoint publishEndpoint,
                      IUserRepository userRepository,
                      IBackgroundJobClient backgroundJobClient,
                      GiupViec3Mien.Services.Elastic.IJobSearchService jobSearchService,
                      INotificationService notificationService,
                      IDistributedLockService distributedLockService)
    {
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _fileStorageService = fileStorageService;
        _publishEndpoint = publishEndpoint;
        _userRepository = userRepository;
        _backgroundJobClient = backgroundJobClient;
        _jobSearchService = jobSearchService;
        _notificationService = notificationService;
        _distributedLockService = distributedLockService;
    }

    public async Task<JobResponse> CreateJobAsync(Guid employerId, CreateJobRequest request)
    {
        ValidateJobSchedule(request);

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
            WorkDate = request.WorkDate?.Date,
            WorkStartTime = request.WorkStartTime,
            WorkEndTime = request.WorkEndTime,
            PreferredGender = request.PreferredGender,
            TargetAgeRange = request.TargetAgeRange
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

        // Elasticsearch Sync
        await _publishEndpoint.Publish(new JobIndexMessage(
            job.Id, job.Title, job.Description, job.ServiceCategory.ToString(), job.Price, 
            job.Latitude, job.Longitude, job.RequiredSkills, job.Status.ToString(), 
            job.PostType.ToString(), job.CreatedAt,
            job.EmployerId, job.Employer?.FullName ?? "Unknown", job.Employer?.AvatarUrl, job.Applications?.Count ?? 0));

        return MapToResponse(job, true);
    }

    public async Task<JobResponse?> UpdateJobAsync(Guid userId, Guid jobId, CreateJobRequest request)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null || job.EmployerId != userId) return null;

        ValidateJobSchedule(request);

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
        job.WorkDate = request.WorkDate?.Date;
        job.WorkStartTime = request.WorkStartTime;
        job.WorkEndTime = request.WorkEndTime;
        job.PreferredGender = request.PreferredGender;
        job.TargetAgeRange = request.TargetAgeRange;
        job.UpdatedAt = DateTime.UtcNow;

        await _jobRepository.SaveChangesAsync();

        // Elasticsearch Sync
        await _publishEndpoint.Publish(new JobIndexMessage(
            job.Id, job.Title, job.Description, job.ServiceCategory.ToString(), job.Price, 
            job.Latitude, job.Longitude, job.RequiredSkills, job.Status.ToString(), 
            job.PostType.ToString(), job.CreatedAt,
            job.EmployerId, job.Employer?.FullName ?? "Unknown", job.Employer?.AvatarUrl, job.Applications?.Count ?? 0));

        return MapToResponse(job, true);
    }

    public async Task<bool> DeleteJobAsync(Guid userId, Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null || job.EmployerId != userId) return false;

        await _jobRepository.DeleteAsync(job);
        await _jobRepository.SaveChangesAsync();

        // Elasticsearch Sync
        await _publishEndpoint.Publish(new JobDeleteMessage(jobId));

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
                            (requester != null && 
                            (requester.Role == Domain.Enums.Role.Admin || 
                            (requester.HasPremiumAccess && requester.PremiumExpiry >= DateTime.UtcNow)));

        // Special case: If the freelancer has applied, they should see the hotline (already in response)
        // and support from company.
        
        return MapToResponse(job, hasFullAccess);
    }

    public async Task<IEnumerable<JobResponse>> GetJobsByEmployerAsync(Guid employerId)
    {
        var jobs = await _jobRepository.GetJobsByEmployerAsync(employerId);
        return jobs.Select(j => MapToResponse(j, true)); // Owner viewing
    }

    public async Task<IEnumerable<JobResponse>> GetJobsByWorkerAsync(Guid workerId)
    {
        var jobs = await _jobRepository.GetByAssignedWorkerIdAsync(workerId);
        return jobs.Select(j => MapToResponse(j, workerId == j.AssignedWorkerId)); // Pass workerId for full access if assigned
    }

    public async Task<IEnumerable<JobResponse>> GetMyAdsAsync(Guid userId)
    {
        // For ads, EmployerId in DB is used as OwnerId
        var jobs = await _jobRepository.GetJobsByEmployerAsync(userId);
        return jobs.Where(j => j.PostType == Domain.Enums.PostType.Seeking).Select(j => MapToResponse(j, true));
    }

    public async Task<JobApplicationResponse> ApplyToJobAsync(Guid userId, Guid jobId, ApplyJobRequest request)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        var applicant = await _userRepository.GetByIdAsync(userId);
        if (applicant == null) throw new Exception("Applicant not found.");
        if (applicant.Role != Domain.Enums.Role.Worker)
        {
            throw new Exception("Chỉ tài khoản người tìm việc mới có thể ứng tuyển.");
        }

        if (job.Status != Domain.Enums.JobStatus.Open || job.AssignedWorkerId.HasValue)
        {
            throw new Exception("Tin tuyển dụng này đã chốt người hoặc không còn nhận ứng tuyển.");
        }

        if (!request.AvailableStartDate.HasValue)
        {
            throw new Exception("Vui lòng chọn ngày bạn có thể bắt đầu làm.");
        }

        if (request.AvailableStartDate.Value.Date < DateTime.Today)
        {
            throw new Exception("Ngày bắt đầu làm không được ở quá khứ.");
        }

        if (job.WorkDate.HasValue && request.AvailableStartDate.Value.Date > job.WorkDate.Value.Date)
        {
            throw new Exception("Ngày bạn có thể bắt đầu làm đang muộn hơn lịch mà chủ nhà yêu cầu.");
        }

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
        await _publishEndpoint.Publish(new JobApplicationTask(userId, jobId, request.Message ?? "", bidPrice, cvUrl, request.AvailableStartDate?.Date));

        // Point 4: Analytics
        await _publishEndpoint.Publish(new AnalyticsEvent("JobApplied", userId, $"JobId: {jobId}"));

        return new JobApplicationResponse
        {
            JobId = jobId,
            ApplicantId = userId,
            Message = request.Message,
            BidPrice = bidPrice,
            CvUrl = cvUrl,
            AvailableStartDate = request.AvailableStartDate?.Date,
            AppliedAt = DateTime.UtcNow,
            IsAccepted = false,
            Status = Domain.Enums.ApplicationStatus.Pending
        };
    }

    public async Task<IEnumerable<JobApplicationResponse>> GetJobApplicationsAsync(Guid jobId, Guid requesterId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester == null) throw new Exception("Requester not found.");

        // Security: Only the Job Owner or Admin can view the applicants list
        if (job.EmployerId != requesterId && requester.Role != Domain.Enums.Role.Admin)
        {
            throw new Exception("You do not have permission to view this applicants list.");
        }

        var applications = await _applicationRepository.GetByJobIdAsync(jobId);
        
        // Privacy: Contact details are only visible to Admin or Premium users
        bool hasFullContactAccess = requester.Role == Domain.Enums.Role.Admin || 
                                   (requester.HasPremiumAccess && requester.PremiumExpiry >= DateTime.UtcNow);

        return applications.Select(a => MapToApplicationResponse(a, hasFullContactAccess || a.ApplicantId == requesterId));
    }

    private JobApplicationResponse MapToApplicationResponse(Domain.Entities.JobApplication a, bool fullAccess)
    {
        return new JobApplicationResponse
        {
            Id = a.Id,
            JobId = a.JobId,
            JobTitle = a.Job?.Title ?? "Unknown Job",
            JobPrice = a.Job?.Price ?? 0,
            ApplicantId = a.ApplicantId,
            ApplicantName = a.Applicant?.FullName ?? "Unknown",
            ApplicantAvatarUrl = a.Applicant?.AvatarUrl,
            ApplicantPhone = fullAccess ? a.Applicant?.Phone : MaskContact(a.Applicant?.Phone),
            ApplicantEmail = fullAccess ? a.Applicant?.Email : MaskContact(a.Applicant?.Email),
            Message = a.Message,
            BidPrice = a.BidPrice,
            CvUrl = a.CvUrl,
            AvailableStartDate = a.AvailableStartDate,
            AppliedAt = a.AppliedAt,
            IsAccepted = a.IsAccepted,
            Status = a.Status
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
        return applications.Select(a => MapToApplicationResponse(a, true)); // Owner accessing
    }

    public async Task<JobApplicationResponse?> GetMyApplicationForJobAsync(Guid userId, Guid jobId)
    {
        var application = await _applicationRepository.GetByApplicantAndJobAsync(userId, jobId);
        if (application == null) return null;

        return MapToApplicationResponse(application, true);
    }

    public async Task<JobApplicationResponse?> AcceptApplicationAsync(Guid employerId, Guid applicationId)
    {
        await using var distributedLock = await _distributedLockService.TryAcquireAsync(
            $"locks:job-application:accept:{applicationId}",
            TimeSpan.FromSeconds(15));

        if (distributedLock == null)
        {
            throw new Exception("Hệ thống đang xử lý thao tác nhận ứng viên này. Vui lòng thử lại sau vài giây.");
        }

        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null) return null;

        var job = await _jobRepository.GetByIdAsync(application.JobId);
        if (job == null || job.EmployerId != employerId) return null;

        if (application.Status == Domain.Enums.ApplicationStatus.Rejected)
        {
            throw new Exception("Không thể nhận một hồ sơ đã bị từ chối.");
        }

        if (application.Status == Domain.Enums.ApplicationStatus.Accepted || application.IsAccepted)
        {
            return MapToApplicationResponse(application, true);
        }

        if (job.AssignedWorkerId.HasValue && job.AssignedWorkerId != application.ApplicantId)
        {
            throw new Exception("Tin tuyển dụng này đã nhận người làm khác.");
        }

        if (job.Status != Domain.Enums.JobStatus.Open && job.Status != Domain.Enums.JobStatus.InProgress)
        {
            throw new Exception("Tin tuyển dụng này không còn ở trạng thái có thể nhận ứng viên.");
        }

        var hasConflict = await HasScheduleConflictAsync(application.ApplicantId, job, job.Id);
        if (hasConflict)
        {
            throw new Exception("Ứng viên này đang có lịch làm trùng giờ với công việc đã được nhận trước đó.");
        }

        // Update application
        application.IsAccepted = true;
        application.Status = Domain.Enums.ApplicationStatus.Accepted;

        // Update job
        job.Status = Domain.Enums.JobStatus.InProgress;
        job.AssignedWorkerId = application.ApplicantId;
        job.UpdatedAt = DateTime.UtcNow;

        var siblingApplications = (await _applicationRepository.GetByJobIdAsync(job.Id))
            .Where(a => a.Id != application.Id && a.Status == Domain.Enums.ApplicationStatus.Pending)
            .ToList();

        foreach (var sibling in siblingApplications)
        {
            sibling.Status = Domain.Enums.ApplicationStatus.Rejected;
            sibling.IsAccepted = false;
        }

        await _applicationRepository.SaveChangesAsync();
        await _jobRepository.SaveChangesAsync();

        // Elasticsearch Sync (Update Job Status)
        await _publishEndpoint.Publish(new JobIndexMessage(
            job.Id, job.Title, job.Description, job.ServiceCategory.ToString(), job.Price, 
            job.Latitude, job.Longitude, job.RequiredSkills, job.Status.ToString(), 
            job.PostType.ToString(), job.CreatedAt,
            job.EmployerId, job.Employer?.FullName ?? "Unknown", job.Employer?.AvatarUrl, job.Applications?.Count ?? 0));

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

        await _notificationService.CreateAsync(
            application.ApplicantId,
            "application_accepted",
            "Đơn ứng tuyển đã được chấp nhận",
            $"Chúc mừng bạn! Tin '{job.Title}' đã chấp nhận hồ sơ của bạn.",
            "/dashboard/viec-da-ung-tuyen");

        foreach (var sibling in siblingApplications)
        {
            await _notificationService.CreateAsync(
                sibling.ApplicantId,
                "application_rejected",
                "Đơn ứng tuyển không được chọn",
                $"Tin '{job.Title}' đã nhận ứng viên khác phù hợp hơn.",
                "/dashboard/viec-da-ung-tuyen");
        }

        return MapToApplicationResponse(application, true);
    }

    public async Task<bool> RejectApplicationAsync(Guid employerId, Guid applicationId)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
        {
            return false;
        }

        var job = application.Job ?? await _jobRepository.GetByIdAsync(application.JobId);
        if (job == null || job.EmployerId != employerId)
        {
            return false;
        }

        if (application.IsAccepted || application.Status == Domain.Enums.ApplicationStatus.Accepted)
        {
            throw new Exception("Khong the tu choi ung vien da duoc nhan.");
        }

        if (application.Status == Domain.Enums.ApplicationStatus.Rejected)
        {
            return true;
        }

        application.Status = Domain.Enums.ApplicationStatus.Rejected;
        application.IsAccepted = false;
        await _applicationRepository.SaveChangesAsync();

        await _publishEndpoint.Publish(new AnalyticsEvent("ApplicationRejected", employerId, $"AppId: {applicationId}"));

        await _notificationService.CreateAsync(
            application.ApplicantId,
            "application_rejected",
            "Đơn ứng tuyển đã bị từ chối",
            $"Rất tiếc, tin '{job.Title}' chưa chọn hồ sơ của bạn ở thời điểm này.",
            "/dashboard/viec-da-ung-tuyen");

        return true;
    }

    public async Task<string> GetApplicationCvUrlAsync(Guid requesterId, Guid applicationId)
    {
        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester == null)
        {
            throw new Exception("Requester not found.");
        }

        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
        {
            throw new Exception("Application not found.");
        }

        if (string.IsNullOrWhiteSpace(application.CvUrl) || application.CvUrl == "[Processing...]")
        {
            throw new Exception("CV is still being processed or has not been uploaded yet.");
        }

        var canViewCv = requester.Role == Domain.Enums.Role.Admin ||
                        application.ApplicantId == requesterId ||
                        application.Job?.EmployerId == requesterId;

        if (!canViewCv)
        {
            throw new Exception("You do not have permission to view this CV.");
        }

        var accessibleUrl = await _fileStorageService.GetAccessibleFileUrlAsync(application.CvUrl);

        if (!string.Equals(accessibleUrl, application.CvUrl, StringComparison.Ordinal))
        {
            application.CvUrl = accessibleUrl;
            await _applicationRepository.SaveChangesAsync();
        }

        return accessibleUrl;
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
            Id = job.Id,
            EmployerId = job.EmployerId,
            EmployerName = job.Employer?.FullName ?? "Unknown",
            EmployerAvatarUrl = job.Employer?.AvatarUrl,
            EmployerPhone = fullAccess ? job.Employer?.Phone : MaskContact(job.Employer?.Phone),
            EmployerEmail = fullAccess ? job.Employer?.Email : MaskContact(job.Employer?.Email),
            CompanyHotline = "1900-xxxx (Giúp Việc 3 Miền Support)",
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
            PostType = job.PostType,
            TimingType = job.TimingType,
            ServiceCategory = job.ServiceCategory,
            WorkingTimeDescription = job.WorkingTimeDescription,
            WorkDate = job.WorkDate,
            WorkStartTime = job.WorkStartTime,
            WorkEndTime = job.WorkEndTime,
            PreferredGender = job.PreferredGender,
            TargetAgeRange = job.TargetAgeRange,
            ApplicantCount = job.Applications?.Count ?? 0,
            AssignedWorkerId = job.AssignedWorkerId,
            AssignedWorkerName = job.AssignedWorker?.FullName,
            CreatedAt = job.CreatedAt
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

        // Elasticsearch Sync
        await _publishEndpoint.Publish(new JobDeleteMessage(jobId));

        await _publishEndpoint.Publish(new Messaging.AnalyticsEvent("JobDeletedByAdmin", Guid.Empty, $"JobId: {jobId}, Reason: Policy Violation"));

        if (!string.IsNullOrEmpty(employerEmail))
        {
            string subject = "Your job post has been removed due to policy violation";
            string body = $"<p>Hi,</p><p>Your job post <strong>\"{jobTitle}\"</strong> has been removed by the administrator as it violates our system policies.</p>";
            await _publishEndpoint.Publish(new Messaging.SendEmailMessage(employerEmail, subject, body));
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
            Id = Guid.Parse(doc.Id),
            EmployerId = doc.EmployerId,
            EmployerName = doc.EmployerName,
            EmployerAvatarUrl = doc.EmployerAvatarUrl,
            CompanyHotline = "1900-xxxx (Giúp Việc 3 Miền Support)",
            Title = doc.Title,
            Description = doc.Description,
            Location = doc.Location,
            Price = doc.Price,
            Latitude = doc.Coordinates?.Lat ?? 0,
            Longitude = doc.Coordinates?.Lon ?? 0,
            RequiredSkills = doc.RequiredSkills,
            Status = Enum.Parse<Domain.Enums.JobStatus>(doc.Status),
            PostType = Enum.Parse<Domain.Enums.PostType>(doc.PostType),
            TimingType = Domain.Enums.JobTimingType.PartTime, // Default or store in ES if needed
            ServiceCategory = Enum.Parse<Domain.Enums.ServiceCategory>(doc.Category),
            ApplicantCount = doc.ApplicantCount,
            CreatedAt = doc.CreatedAt
        };
    }

    private static void ValidateJobSchedule(CreateJobRequest request)
    {
        if (!request.WorkDate.HasValue)
        {
            throw new Exception("Vui lòng chọn ngày làm việc.");
        }

        if (request.WorkDate.Value.Date < DateTime.Today)
        {
            throw new Exception("Ngày làm việc không được ở quá khứ.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkStartTime))
        {
            throw new Exception("Vui lòng chọn giờ bắt đầu.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkEndTime))
        {
            throw new Exception("Vui lòng chọn giờ kết thúc.");
        }

        if (!TimeSpan.TryParse(request.WorkStartTime, out var startTime) ||
            !TimeSpan.TryParse(request.WorkEndTime, out var endTime))
        {
            throw new Exception("Khung giờ làm việc không hợp lệ.");
        }

        if (startTime >= endTime)
        {
            throw new Exception("Giờ kết thúc phải sau giờ bắt đầu.");
        }
    }

    private async Task<bool> HasScheduleConflictAsync(Guid workerId, Domain.Entities.Job targetJob, Guid currentJobId)
    {
        if (!targetJob.WorkDate.HasValue ||
            string.IsNullOrWhiteSpace(targetJob.WorkStartTime) ||
            string.IsNullOrWhiteSpace(targetJob.WorkEndTime))
        {
            return false;
        }

        if (!TimeSpan.TryParse(targetJob.WorkStartTime, out var targetStart) ||
            !TimeSpan.TryParse(targetJob.WorkEndTime, out var targetEnd))
        {
            return false;
        }

        var currentJobs = await _jobRepository.GetByAssignedWorkerIdAsync(workerId);

        foreach (var scheduledJob in currentJobs.Where(j => j.Id != currentJobId && j.WorkDate.HasValue))
        {
            if (scheduledJob.WorkDate!.Value.Date != targetJob.WorkDate.Value.Date)
            {
                continue;
            }

            if (!TimeSpan.TryParse(scheduledJob.WorkStartTime, out var scheduledStart) ||
                !TimeSpan.TryParse(scheduledJob.WorkEndTime, out var scheduledEnd))
            {
                continue;
            }

            if (targetStart < scheduledEnd && scheduledStart < targetEnd)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<int> GetTotalApplicationCountAsync()
    {
        return await _applicationRepository.CountAsync();
    }

    public async Task<JobResponse?> UpdateJobStatusAsync(Guid jobId, Domain.Enums.JobStatus newStatus)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;

        job.Status = newStatus;
        job.UpdatedAt = DateTime.UtcNow;

        await _jobRepository.SaveChangesAsync();

        // Elasticsearch Sync
        await _publishEndpoint.Publish(new JobIndexMessage(
            job.Id, job.Title, job.Description, job.ServiceCategory.ToString(), job.Price, 
            job.Latitude, job.Longitude, job.RequiredSkills, job.Status.ToString(), 
            job.PostType.ToString(), job.CreatedAt,
            job.EmployerId, job.Employer?.FullName ?? "Unknown", job.Employer?.AvatarUrl, job.Applications?.Count ?? 0));

        return MapToResponse(job, true);
    }

    public async Task<IDictionary<Domain.Enums.JobStatus, int>> GetJobStatusCountsAsync()
    {
        var jobs = await _jobRepository.GetAllAsync();
        return jobs
            .GroupBy(j => j.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    
    public async Task ReindexAllJobsAsync()
    {
        await _jobSearchService.InitializeIndexAsync();
        
        var jobs = await _jobRepository.GetAllAsync();
        
        var documents = jobs.Select(job => new Elastic.JobDocument
        {
            Id = job.Id.ToString(),
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            Category = job.ServiceCategory.ToString(),
            Price = job.Price,
            Coordinates = new global::Elastic.Clients.Elasticsearch.LatLonGeoLocation { Lat = job.Latitude, Lon = job.Longitude },
            RequiredSkills = string.IsNullOrEmpty(job.RequiredSkills) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(job.RequiredSkills) ?? new List<string>(),
            Status = job.Status.ToString(),
            PostType = job.PostType.ToString(),
            CreatedAt = job.CreatedAt,
            EmployerId = job.EmployerId,
            EmployerName = job.Employer?.FullName ?? "Unknown",
            EmployerAvatarUrl = job.Employer?.AvatarUrl,
            ApplicantCount = job.Applications?.Count ?? 0
        }).ToList();

        if (documents.Any())
        {
            await _jobSearchService.BulkIndexAsync(documents);
        }
    }
}
