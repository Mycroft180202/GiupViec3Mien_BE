using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using GiupViec3Mien.Services.BackgroundJobs;
using System;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public AdminController(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    /// <summary>
    /// Manually trigger the daily job expiration cleanup logic.
    /// </summary>
    [HttpPost("trigger-job-cleanup")]
    public IActionResult TriggerJobCleanup()
    {
        var jobId = _backgroundJobClient.Enqueue<JobExpirationJob>(
            x => x.ExecuteAsync());
            
        return Ok(new { HangfireJobId = jobId, Message = "System cleanup job enqueued." });
    }

    /// <summary>
    /// Manually trigger the weekly newsletter broadcast.
    /// </summary>
    [HttpPost("trigger-newsletter")]
    public IActionResult TriggerNewsletter()
    {
        var jobId = _backgroundJobClient.Enqueue<NewsletterJob>(
            x => x.ExecuteAsync());
            
        return Ok(new { HangfireJobId = jobId, Message = "Manual newsletter broadcast started." });
    }

    /// <summary>
    /// Update or reset the weekly newsletter recurring schedule.
    /// </summary>
    [HttpPost("setup-recurring-newsletter")]
    public IActionResult SetupNewsletterSchedule()
    {
        _recurringJobManager.AddOrUpdate<NewsletterJob>(
            "weekly-newsletter", 
            x => x.ExecuteAsync(), 
            Cron.Weekly(DayOfWeek.Monday, 9)); // Every Monday at 9:00 AM
            
        return Ok(new { Message = "Newsletter schedule updated to every Monday at 9:00 AM." });
    }
}
