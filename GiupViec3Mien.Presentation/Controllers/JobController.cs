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

    [HttpGet]
    public async Task<IActionResult> GetAvailableJobs()
    {
        var jobs = await _jobService.GetAvailableJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobDetails(Guid id)
    {
        var job = await _jobService.GetJobDetailsAsync(id);
        if (job == null) return NotFound(new { message = "Công việc không tồn tại." });

        return Ok(job);
    }
}
