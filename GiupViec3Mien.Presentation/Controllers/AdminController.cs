using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using GiupViec3Mien.Services.BackgroundJobs;
using GiupViec3Mien.Services.UserServices;
using GiupViec3Mien.Services.Job;
using GiupViec3Mien.Services.Subscription;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.Admin;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IUserService _userService;
    private readonly IJobService _jobService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IActivityLogRepository _activityLogRepository;

    public AdminController(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        IUserService userService,
        IJobService jobService,
        ISubscriptionService subscriptionService,
        IActivityLogRepository activityLogRepository)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _userService = userService;
        _jobService = jobService;
        _subscriptionService = subscriptionService;
        _activityLogRepository = activityLogRepository;
    }

    // ═══════════════════════════════════════════════════════
    // SECTION 1 – USER ACCOUNT MANAGEMENT
    // ═══════════════════════════════════════════════════════

    /// <summary>List all user accounts (summary view).</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users.Select(u => new
        {
            u.Id,
            u.Phone,
            u.Email,
            u.FullName,
            u.Role,
            u.IsGuest,
            u.IsLocked,
            u.HasPremiumAccess,
            u.PremiumExpiry,
            u.CreatedAt
        }));
    }

    /// <summary>Get full details for a single user account.</summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserDetail(Guid id)
    {
        var user = await _userService.GetUserDetailAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
        return Ok(user);
    }

    /// <summary>Update user account fields (role, premium access, locked state, etc.).</summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, AdminUpdateUserRequest request)
    {
        var updated = await _userService.UpdateUserAsync(id, request);
        if (updated == null) return NotFound(new { message = "User not found." });

        await LogActivityAsync("UpdateUser", "User", id,
            $"Admin updated user account: {updated.FullName} ({updated.Phone})");

        return Ok(updated);
    }

    /// <summary>
    /// Permanently delete a user account.
    /// Use with caution – this hard-deletes the user and cascades to WorkerProfile.
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        // Prevent self-deletion
        var adminId = GetCurrentAdminId();
        if (adminId == id)
            return BadRequest(new { message = "Administrators cannot delete their own account." });

        var success = await _userService.DeleteUserAsync(id);
        if (!success) return NotFound(new { message = "User not found." });

        await LogActivityAsync("DeleteUser", "User", id,
            $"Admin permanently deleted user account (id={id}).");

        return Ok(new { Message = "User account permanently deleted." });
    }

    /// <summary>Lock / suspend a user account.</summary>
    [HttpPost("users/{id}/lock")]
    public async Task<IActionResult> LockUser(Guid id)
    {
        try
        {
            await _userService.LockUserAsync(id);
            await LogActivityAsync("LockUser", "User", id,
                $"Admin locked user account (id={id}).");
            return Ok(new { Message = "Tài khoản đã bị khóa thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Unlock / restore a suspended user account.</summary>
    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        try
        {
            await _userService.UnlockUserAsync(id);
            await LogActivityAsync("UnlockUser", "User", id,
                $"Admin unlocked user account (id={id}).");
            return Ok(new { Message = "Tài khoản đã được mở khóa thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════
    // SECTION 2 – JOB POST MANAGEMENT
    // ═══════════════════════════════════════════════════════

    /// <summary>List all job postings and seeking ads (all statuses).</summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs = await _jobService.GetAllJobsAsync();
        return Ok(jobs);
    }

    /// <summary>
    /// Remove a violating job post.
    /// Sends an email notification to the employer and logs the action.
    /// </summary>
    [HttpDelete("jobs/{id}")]
    public async Task<IActionResult> DeleteViolatingJob(Guid id)
    {
        var success = await _jobService.DeleteViolatingJobAsync(id);
        if (!success) return NotFound(new { message = "Tin đăng không tồn tại." });

        await LogActivityAsync("DeleteViolatingJob", "Job", id,
            $"Admin xóa tin đăng vi phạm (id={id}).");

        return Ok(new { Message = "Tin đăng vi phạm đã được xóa." });
    }

    /// <summary>
    /// Override the status of a job post (Open / InProgress / Completed / Cancelled).
    /// Useful for resolving disputes or force-completing stale jobs.
    /// </summary>
    [HttpPatch("jobs/{id}/status")]
    public async Task<IActionResult> UpdateJobStatus(Guid id, [FromBody] UpdateJobStatusRequest request)
    {
        var updated = await _jobService.UpdateJobStatusAsync(id, request.Status);
        if (updated == null) return NotFound(new { message = "Tin đăng không tồn tại." });

        await LogActivityAsync("UpdateJobStatus", "Job", id,
            $"Admin cập nhật trạng thái tin đăng thành '{request.Status}' (id={id}).");

        return Ok(updated);
    }

    // ═══════════════════════════════════════════════════════
    // SECTION 3 – SUBSCRIPTION PACKAGE MANAGEMENT
    // ═══════════════════════════════════════════════════════

    /// <summary>List all service packages including inactive ones.</summary>
    [HttpGet("packages")]
    public async Task<IActionResult> GetAllPackages()
    {
        var packages = await _subscriptionService.GetAllPackagesAsync(includeInactive: true);
        return Ok(packages);
    }

    /// <summary>Create a new subscription package.</summary>
    [HttpPost("packages")]
    public async Task<IActionResult> CreatePackage(GiupViec3Mien.Services.DTOs.Subscription.SubscriptionPackageRequest request)
    {
        var package = await _subscriptionService.CreatePackageAsync(request);
        await LogActivityAsync("CreatePackage", "Package", package.Id,
            $"Admin tạo gói dịch vụ mới: '{package.Name}' (giá {package.Price}).");
        return CreatedAtAction(nameof(GetAllPackages), new { id = package.Id }, package);
    }

    /// <summary>Update an existing service package.</summary>
    [HttpPut("packages/{id}")]
    public async Task<IActionResult> UpdatePackage(Guid id, GiupViec3Mien.Services.DTOs.Subscription.SubscriptionPackageRequest request)
    {
        var package = await _subscriptionService.UpdatePackageAsync(id, request);
        if (package == null) return NotFound(new { message = "Gói dịch vụ không tồn tại." });

        await LogActivityAsync("UpdatePackage", "Package", id,
            $"Admin cập nhật gói dịch vụ: '{package.Name}'.");
        return Ok(package);
    }

    /// <summary>Toggle a package's active/inactive state without deleting it.</summary>
    [HttpPatch("packages/{id}/toggle-active")]
    public async Task<IActionResult> TogglePackageActive(Guid id)
    {
        var package = await _subscriptionService.TogglePackageActiveAsync(id);
        if (package == null) return NotFound(new { message = "Gói dịch vụ không tồn tại." });

        string state = package.IsActive ? "kích hoạt" : "tắt";
        await LogActivityAsync("TogglePackage", "Package", id,
            $"Admin {state} gói dịch vụ: '{package.Name}'.");
        return Ok(package);
    }

    /// <summary>Permanently delete a service package.</summary>
    [HttpDelete("packages/{id}")]
    public async Task<IActionResult> DeletePackage(Guid id)
    {
        var success = await _subscriptionService.DeletePackageAsync(id);
        if (!success) return NotFound(new { message = "Gói dịch vụ không tồn tại." });

        await LogActivityAsync("DeletePackage", "Package", id,
            $"Admin xóa gói dịch vụ (id={id}).");
        return Ok(new { Message = "Gói dịch vụ đã được xóa." });
    }

    // ═══════════════════════════════════════════════════════
    // SECTION 4 – SYSTEM ACTIVITY MONITORING
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Dashboard stats: users, jobs, applications, premium, locked accounts,
    /// job status breakdown, and package info.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var users = await _userService.GetAllUsersAsync();
        var jobs = await _jobService.GetAllJobsAsync();
        var applicationsCount = await _jobService.GetTotalApplicationCountAsync();
        var jobStatusCountsRaw = await _jobService.GetJobStatusCountsAsync();
        IReadOnlyDictionary<JobStatus, int> jobStatusCounts = new System.Collections.ObjectModel.ReadOnlyDictionary<JobStatus, int>(jobStatusCountsRaw);
        var subscriptionStats = await _subscriptionService.GetSubscriptionStatsAsync();
        var activityCount = await _activityLogRepository.CountAsync();

        var now = DateTime.UtcNow;
        var userList = users.ToList();
        var jobList = jobs.ToList();

        return Ok(new AdminSystemStatsResponse
        {
            TotalUsers = userList.Count,
            TotalWorkers = userList.Count(u => u.Role == Role.Worker),
            TotalEmployers = userList.Count(u => u.Role == Role.Employer),
            LockedAccounts = userList.Count(u => u.IsLocked),
            ActivePremiumUsers = userList.Count(u => u.HasPremiumAccess && u.PremiumExpiry > now),
            TotalJobs = jobList.Count,
            OpenJobs = jobStatusCounts.GetValueOrDefault(JobStatus.Open, 0),
            InProgressJobs = jobStatusCounts.GetValueOrDefault(JobStatus.InProgress, 0),
            CompletedJobs = jobStatusCounts.GetValueOrDefault(JobStatus.Completed, 0),
            TotalApplications = applicationsCount,
            TotalPackages = subscriptionStats.TotalPackages,
            ActivePackages = subscriptionStats.ActivePackages,
            SystemHealthy = true,
            LastUpdate = now
        });
    }

    /// <summary>
    /// Activity log: paginated list of admin actions for auditing.
    /// </summary>
    [HttpGet("activity-logs")]
    public async Task<IActionResult> GetActivityLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (pageSize > 200) pageSize = 200;
        var logs = await _activityLogRepository.GetAllAsync(page, pageSize);
        var total = await _activityLogRepository.CountAsync();

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Data = logs.Select(l => new ActivityLogResponse
            {
                Id = l.Id,
                ActorId = l.ActorId,
                ActorName = l.Actor?.FullName,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Description = l.Description,
                Metadata = l.Metadata,
                CreatedAt = l.CreatedAt
            })
        });
    }

    /// <summary>Activity log for a specific entity (e.g. all actions on a given user or job).</summary>
    [HttpGet("activity-logs/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityActivityLogs(string entityType, Guid entityId)
    {
        var logs = await _activityLogRepository.GetByEntityAsync(entityType, entityId);
        return Ok(logs.Select(l => new ActivityLogResponse
        {
            Id = l.Id,
            ActorId = l.ActorId,
            ActorName = l.Actor?.FullName,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Description = l.Description,
            Metadata = l.Metadata,
            CreatedAt = l.CreatedAt
        }));
    }

    // ═══════════════════════════════════════════════════════
    // SECTION 5 – BACKGROUND JOB TRIGGERS
    // ═══════════════════════════════════════════════════════

    /// <summary>Manually trigger the daily job expiration cleanup logic.</summary>
    [HttpPost("trigger-job-cleanup")]
    public IActionResult TriggerJobCleanup()
    {
        var jobId = _backgroundJobClient.Enqueue<JobExpirationJob>(x => x.ExecuteAsync());
        return Ok(new { HangfireJobId = jobId, Message = "Tác vụ dọn dẹp hệ thống đã được xếp hàng." });
    }

    /// <summary>Manually trigger the weekly newsletter broadcast.</summary>
    [HttpPost("trigger-newsletter")]
    public IActionResult TriggerNewsletter()
    {
        var jobId = _backgroundJobClient.Enqueue<NewsletterJob>(x => x.ExecuteAsync());
        return Ok(new { HangfireJobId = jobId, Message = "Bản tin định kỳ đã được khởi động thủ công." });
    }

    // ═══════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════

    private Guid? GetCurrentAdminId()
    {
        var claim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : (Guid?)null;
    }

    private async Task LogActivityAsync(string action, string entityType, Guid? entityId, string description)
    {
        var log = new ActivityLog
        {
            ActorId = GetCurrentAdminId(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description
        };
        await _activityLogRepository.AddAsync(log);
        await _activityLogRepository.SaveChangesAsync();
    }
}

/// <summary>Request body for PATCH /api/admin/jobs/{id}/status</summary>
public record UpdateJobStatusRequest(JobStatus Status);
