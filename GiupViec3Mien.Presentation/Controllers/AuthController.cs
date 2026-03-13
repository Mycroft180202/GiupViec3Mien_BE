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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
    public async Task<IActionResult> GuestCheckout([FromQuery] string phone, [FromQuery] string fullName)
    {
        try
        {
            var response = await _authService.GuestCheckoutAsync(phone, fullName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
