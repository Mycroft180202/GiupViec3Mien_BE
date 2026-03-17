using GiupViec3Mien.Services.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GiupViec3Mien.Services.UserServices;

namespace GiupViec3Mien.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("uploadprofile")]
    public async Task<IActionResult> UploadProfile([FromBody] UploadProfileRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User not identified." });
            }

            if (request == null || string.IsNullOrEmpty(request.ImgUrl))
            {
                return BadRequest(new { message = "No image URL provided." });
            }

            var userId = Guid.Parse(userIdClaim);
            await _userService.UpdateProfileImageUrlAsync(userId, request.ImgUrl);

            return Ok(new { imgurl = request.ImgUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("uploadfile")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            
            if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

            var userId = Guid.Parse(userIdClaim);
            var imageUrl = await _userService.UploadProfileImageAsync(userId, file);

            return Ok(new { imgurl = imageUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("updateskill")]
    public async Task<IActionResult> UpdateSkill([FromBody] List<string> skills)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            await _userService.UpdateSkillsAsync(userId, skills);

            return Ok(new { message = "Skills updated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("updateemail")]
    public async Task<IActionResult> UpdateEmail([FromBody] string email)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            await _userService.UpdateEmailAsync(userId, email);

            return Ok(new { message = "Email updated successfully.", email = email });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
