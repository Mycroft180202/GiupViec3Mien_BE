using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using GiupViec3Mien.Services.BackgroundJobs;
using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Job;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobController(IJobService jobService, IBackgroundJobClient backgroundJobClient)
    {
        _jobService = jobService;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var employerId))
        {
            return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
        }

        request.PostType = PostType.Hiring;

        try
        {
            var response = await _jobService.CreateJobAsync(employerId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("seeking")]
    [Authorize(Roles = "Worker,Admin")]
    public async Task<IActionResult> CreateSeekingAd([FromBody] CreateJobRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var workerId)) return Unauthorized();

        request.PostType = PostType.Seeking;

        try
        {
            var response = await _jobService.CreateJobAsync(workerId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] CreateJobRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var response = await _jobService.UpdateJobAsync(userId, id, request);
        if (response == null) return NotFound(new { message = "Entry not found or permission denied." });

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var success = await _jobService.DeleteJobAsync(userId, id);
        if (!success) return NotFound(new { message = "Entry not found or permission denied." });

        return NoContent();
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableJobs()
    {
        var jobs = await _jobService.GetAvailableJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("worker-ads")]
    public async Task<IActionResult> GetWorkerAds()
    {
        var ads = await _jobService.GetWorkerAdsAsync();
        return Ok(ads);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchJobs([FromQuery] JobSearchFilters filters)
    {
        var jobs = await _jobService.SearchJobsAsync(filters);
        return Ok(jobs);
    }
    
    [HttpGet("filter-by-skills")]
    public async Task<IActionResult> FilterBySkills([FromQuery] string skills)
    {
        if (string.IsNullOrEmpty(skills)) 
            return await GetAvailableJobs();

        var skillList = skills.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
        var jobs = await _jobService.GetJobsBySkillsAsync(skillList);
        return Ok(jobs);
    }

    [HttpGet("my-jobs")]
    [Authorize]
    public async Task<IActionResult> GetMyJobs()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var jobs = await _jobService.GetJobsByEmployerAsync(userId);
        return Ok(jobs);
    }

    [HttpGet("my-ads")]
    [Authorize]
    public async Task<IActionResult> GetMyAds()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var ads = await _jobService.GetMyAdsAsync(userId);
        return Ok(ads);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobDetails(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdString, out var requesterId);

        var job = await _jobService.GetJobDetailsAsync(id, requesterId);
        if (job == null) return NotFound(new { message = "Công việc không tồn tại." });

        return Ok(job);
    }

    [HttpPost("{id}/apply")]
    [Authorize]
    public async Task<IActionResult> ApplyJob(Guid id, [FromForm] ApplyJobRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        try
        {
            var response = await _jobService.ApplyToJobAsync(userId, id, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/applicants")]
    [Authorize]
    public async Task<IActionResult> GetJobApplicants(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var requesterId)) return Unauthorized();

        try
        {
            var applicants = await _jobService.GetJobApplicationsAsync(id, requesterId);
            return Ok(applicants);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-applications")]
    [Authorize]
    public async Task<IActionResult> GetMyApplications()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        if (User.IsInRole("Employer"))
        {
            var received = await _jobService.GetReceivedApplicationsAsync(userId);
            return Ok(received);
        }

        var applications = await _jobService.GetMyApplicationsAsync(userId);
        return Ok(applications);
    }

    [HttpGet("{id}/my-application")]
    [Authorize]
    public async Task<IActionResult> GetMyApplicationForJob(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var application = await _jobService.GetMyApplicationForJobAsync(userId, id);
        if (application == null) return NotFound(new { message = "You have not applied to this job." });

        return Ok(application);
    }

    [HttpPost("applications/{applicationId}/accept")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<IActionResult> AcceptApplication(Guid applicationId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var employerId)) return Unauthorized();

        var response = await _jobService.AcceptApplicationAsync(employerId, applicationId);
        if (response == null) return BadRequest(new { message = "Failed to accept application. It might not exist or you don't have permission." });

        return Ok(response);
    }

    /// <summary>
    /// Manually trigger worker-job matching calculation for a specific job.
    /// Useful for administrators or when the automatic trigger failed.
    /// </summary>
    [HttpPost("{id}/trigger-matching")]
    [Authorize(Roles = "Admin")]
    public IActionResult TriggerMatching(Guid id, [FromQuery] string title)
    {
        var jobId = _backgroundJobClient.Enqueue<JobMatchingJob>(
            x => x.ExecuteAsync(id, title));
            
        return Ok(new { HangfireJobId = jobId, Message = "Matching calculation started in background." });
    }

    /// <summary>
    /// Manually trigger CV processing for an application.
    /// Useful for re-processing or when the initial upload failed.
    /// </summary>
    [HttpPost("applications/{applicationId}/reprocess-cv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReprocessCv(Guid applicationId, [FromForm] Microsoft.AspNetCore.Http.IFormFile cv)
    {
        if (cv == null || cv.Length == 0) return BadRequest(new { message = "CV file is required." });

        // Get app details to find user and job
        var applications = await _jobService.GetMyApplicationsAsync(Guid.Empty); // Dummy call if we don't have a GetById
        // For simplicity in this demo, we use provided bytes
        
        using var ms = new MemoryStream();
        await cv.CopyToAsync(ms);
        
        // We'd need userId and jobId, assuming we get them from internal logic
        // For now, let's keep it generic
        var jobId = _backgroundJobClient.Enqueue<ProcessCVJob>(
            x => x.ExecuteAsync(Guid.Empty, Guid.Empty, ms.ToArray(), cv.FileName));

        return Ok(new { HangfireJobId = jobId, Message = "CV re-processing started." });
    }
}
