using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GiupViec3Mien.Services.Interfaces;

namespace GiupViec3Mien.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IJobRepository _jobRepository;

    public FeedbackController(IReviewRepository reviewRepository, IJobRepository jobRepository)
    {
        _reviewRepository = reviewRepository;
        _jobRepository = jobRepository;
    }

    [HttpGet("{jobId}/{employerId}/rating")]
    public async Task<IActionResult> GetFeedbackRating(Guid jobId, Guid employerId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var currentUserId = Guid.Parse(userIdClaim);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null) return NotFound(new { message = "Job not found." });

            // Check if user is either the employer or the assigned worker
            if (job.EmployerId != currentUserId && job.AssignedWorkerId != currentUserId)
            {
                return Forbid();
            }

            var review = await _reviewRepository.GetReviewAsync(jobId, currentUserId, employerId);
            
            return Ok(new 
            { 
                score = review?.Rating ?? 0 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
