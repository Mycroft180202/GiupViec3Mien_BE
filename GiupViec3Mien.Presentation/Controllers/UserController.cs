using GiupViec3Mien.Services.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GiupViec3Mien.Services.UserServices;
using Hangfire;
using GiupViec3Mien.Services.BackgroundJobs;
using GiupViec3Mien.Services.Interfaces;

namespace GiupViec3Mien.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IVerificationService _verificationService;

    public UserController(IUserService userService, IBackgroundJobClient backgroundJobClient, IVerificationService verificationService)
    {
        _userService = userService;
        _backgroundJobClient = backgroundJobClient;
        _verificationService = verificationService;
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

    /// <summary>
    /// Manually send a profile completion reminder to a worker.
    /// </summary>
    [HttpPost("{userId}/send-reminder")]
    [Authorize(Roles = "Admin")]
    public IActionResult SendProfileReminder(Guid userId)
    {
        var jobId = _backgroundJobClient.Enqueue<ProfileReminderJob>(
            x => x.SendReminderAsync(userId));
            
        return Ok(new { HangfireJobId = jobId, Message = "Profile completion reminder sent in background." });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null) return NotFound(new { message = "User not found." });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            await _userService.UpdateProfileAsync(userId, request);

            return Ok(new { message = "Profile updated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("phone-verification/send")]
    public async Task<IActionResult> SendPhoneVerificationOtp()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null) return NotFound(new { message = "User not found." });

            await _verificationService.GenerateAndSendOtpAsync(profile.Phone);
            return Ok(new { message = "Mã OTP đã được gửi qua Zalo OA/SMS tới số điện thoại hiện tại của bạn." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("phone-verification/verify")]
    public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null) return NotFound(new { message = "User not found." });

            if (string.IsNullOrWhiteSpace(request.OtpCode) || !_verificationService.VerifyOtp(profile.Phone, request.OtpCode))
            {
                return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn." });
            }

            await _userService.MarkPhoneVerifiedAsync(userId, "ZaloOA");
            return Ok(new { message = "Số điện thoại đã được xác minh thành công qua Zalo OA.", channel = "ZaloOA" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("freelancer/{workerId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFreelancerInfo(Guid workerId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdString, out var requesterId);

        var info = await _userService.GetWorkerInfoAsync(workerId, requesterId);
        if (info == null) return NotFound(new { message = "Worker not found." });

        return Ok(info);
    }
}
