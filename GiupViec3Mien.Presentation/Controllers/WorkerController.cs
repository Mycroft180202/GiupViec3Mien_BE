using GiupViec3Mien.Services.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GiupViec3Mien.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkerController : ControllerBase
{
    private readonly IJobService _jobService;

    public WorkerController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet("jobschedule")]
    public async Task<IActionResult> GetJobSchedule()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var workerId)) return Unauthorized();

            var schedule = await _jobService.GetJobsByWorkerAsync(workerId);
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
