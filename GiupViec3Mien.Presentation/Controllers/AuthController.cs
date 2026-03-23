using System;
using System.Threading.Tasks;
using GiupViec3Mien.Services.Auth;
using GiupViec3Mien.Services.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly GiupViec3Mien.Services.Interfaces.IVerificationService _verificationService;

    public AuthController(IAuthService authService, GiupViec3Mien.Services.Interfaces.IVerificationService verificationService)
    {
        _authService = authService;
        _verificationService = verificationService;
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        try
        {
            await _verificationService.GenerateAndSendOtpAsync(request.Phone);
            return Ok(new { message = "Mã xác nhận OTP đã được gửi về Số điện thoại của bạn qua tin nhắn Zalo/SMS." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Không thể gửi OTP: {ex.Message}" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("guest-checkout")]
    public async Task<IActionResult> GuestCheckout([FromBody] GuestCheckoutRequest request)
    {
        try
        {
            var response = await _authService.GuestCheckoutAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
