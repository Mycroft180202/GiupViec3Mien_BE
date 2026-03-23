using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.DTOs.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GiupViec3Mien.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;

    public MatchingController(IMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpGet("best-matches/{jobId}")]
    [HttpGet("{jobId}/bestmatch")]
    public async Task<IActionResult> GetBestMatches(Guid jobId, [FromQuery] int limit = 10)
    {
        try
        {
            // First check if the provided ID is a Worker ID
            // If it is, we find the best Jobs/Employers for this worker
            var workerMatches = await _matchingService.GetBestJobsForWorkerAsync(jobId, limit).ContinueWith(t => t.IsFaulted ? null : t.Result);
            
            if (workerMatches != null && workerMatches.Any())
            {
                return Ok(workerMatches);
            }

            // Otherwise, assume it's a Job ID and find the best Workers for it
            var matches = await _matchingService.GetBestMatchesForJobAsync(jobId, limit);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            // If the worker logic failed because it wasn't a worker, try the job logic
            try 
            {
                var matches = await _matchingService.GetBestMatchesForJobAsync(jobId, limit);
                return Ok(matches);
            }
            catch
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    [HttpGet("{jobId}/topmatch")]
    public async Task<IActionResult> GetTopMatches(Guid jobId, [FromQuery] int limit = 10)
    {
        try
        {
            var results = await _matchingService.GetBestMatchesForJobAsync(jobId, limit);
            // Specifically returning a list of workerIds and scores as per request
            return Ok(results.Select(r => new { r.WorkerId, r.MatchScore }));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId}/{employerId}/rating")]
    public async Task<IActionResult> GetEmployerRatingCombined(Guid jobId, Guid employerId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var currentUserId = Guid.Parse(userIdClaim);

            var result = await _matchingService.GetEmployerRatingAsync(jobId, currentUserId, employerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId}/{employerId}/distance")]
    public async Task<IActionResult> GetDistanceKm(Guid jobId, Guid employerId)
    {
        try
        {
            var distance = await _matchingService.GetDistanceKmAsync(jobId, employerId);
            return Ok(new { jobId = jobId, employerId = employerId, distanceKm = distance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("bestmatch")]
    public async Task<IActionResult> GetBestMatchForEmployer([FromQuery] int limit = 10)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            
            // Check user role to decide which matching to perform
            // Note: This assumes we can infer role from repository or claims.
            // For now, we try both or check the service logic.
            try 
            {
                var workerMatches = await _matchingService.GetBestJobsForWorkerAsync(userId, limit);
                return Ok(workerMatches);
            }
            catch
            {
                var matches = await _matchingService.GetBestMatchesForEmployerAsync(userId, limit);
                return Ok(matches);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId}/distance")]
    public async Task<IActionResult> GetDistance(Guid jobId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var distance = await _matchingService.CalculateDistanceAsync(userId, jobId);
            
            return Ok(new { jobId = jobId, distanceKm = distance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }



    [HttpGet("{jobId}/{employerId}/matchskill")]
    public async Task<IActionResult> GetSkillMatch(Guid jobId, Guid employerId)
    {
        try
        {
            var matchedSkills = await _matchingService.GetSkillMatchAsync(employerId, jobId);
            return Ok(new { jobId = jobId, employerId = employerId, matchedSkills = matchedSkills });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{employerId}/experience")]
    public async Task<IActionResult> GetEmployerExperience(Guid employerId)
    {
        try
        {
            var experience = await _matchingService.GetEmployerExperienceAsync(employerId);
            return Ok(experience);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId}/{employerId}/budgetrate")]
    public async Task<IActionResult> GetBudgetRate(Guid jobId, Guid employerId)
    {
        try
        {
            var budgetFit = await _matchingService.GetBudgetFitScoreAsync(employerId, jobId);
            return Ok(new 
            { 
                jobId = jobId, 
                employerId = employerId, 
                budgetFitScore = budgetFit 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/{employerId}/avgrating")]
    public async Task<IActionResult> GetAverageRating(Guid employerId)
    {
        try
        {
            var rating = await _matchingService.GetUserRatingAsync(employerId);
            return Ok(new { employerId = employerId, averageRating = rating });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("nearest/{employerId}/distance")]
    [Authorize]
    public async Task<IActionResult> GetNearestWorkers(Guid employerId, [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int limit = 10)



    {
        try
        {
            var results = await _matchingService.GetNearestWorkersByDistanceAsync(lat, lng, limit);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

