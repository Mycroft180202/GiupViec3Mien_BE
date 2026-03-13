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
            var matches = await _matchingService.GetBestMatchesForJobAsync(jobId, limit);
            return Ok(matches);
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

            var employerId = Guid.Parse(userIdClaim);
            var matches = await _matchingService.GetBestMatchesForEmployerAsync(employerId, limit);
            return Ok(matches);
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

    [HttpGet("{jobId}/{employerId}/distance")]
    public async Task<IActionResult> GetDistanceExplicit(Guid jobId, Guid employerId)
    {
        try
        {
            var distance = await _matchingService.CalculateDistanceAsync(employerId, jobId);
            return Ok(new { jobId = jobId, employerId = employerId, distanceKm = distance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId}/{employerId}/rating")]
    public async Task<IActionResult> GetEmployerRating(Guid jobId, Guid employerId)
    {
        try
        {
            var rating = await _matchingService.GetUserRatingAsync(employerId);
            return Ok(new 
            { 
                jobId = jobId, 
                employerId = employerId, 
                averageRating = rating 
            });
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
}
