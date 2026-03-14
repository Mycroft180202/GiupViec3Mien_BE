using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobController(IJobService jobService)
    {
        _jobService = jobService;
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

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableJobs()
    {
        var jobs = await _jobService.GetAvailableJobsAsync();
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobDetails(Guid id)
    {
        var job = await _jobService.GetJobDetailsAsync(id);
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
        try
        {
            var applicants = await _jobService.GetJobApplicationsAsync(id);
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
}
