using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.Auth;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Hangfire;

namespace GiupViec3Mien.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, IBackgroundJobClient backgroundJobClient)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByPhoneAsync(request.Phone);
        if (existingUser != null)
        {
            throw new Exception("Số điện thoại đã được đăng ký.");
        }

        var user = new User
        {
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role
        };

        if (request.Role == Role.Worker)
        {
            user.WorkerProfile = new WorkerProfile();
        }

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Follow-up Reminder (scheduled for 2 hours later)
        if (request.Role == Role.Worker)
        {
            _backgroundJobClient.Schedule<BackgroundJobs.ProfileReminderJob>(
                job => job.SendReminderAsync(user.Id), 
                TimeSpan.FromHours(2)
            );
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByPhoneAsync(request.Phone);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new Exception("Số điện thoại hoặc mật khẩu không đúng.");
        }

        if (user.IsLocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> GuestCheckoutAsync(string phone, string fullName)
    {
        var user = await _userRepository.GetByPhoneAsync(phone);
        
        // Shadow Account Logic
        if (user == null)
        {
            user = new User
            {
                Phone = phone,
                FullName = fullName,
                Role = Role.Employer,
                IsGuest = true,
                // Random shadow password
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
            };
            
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role,
            Phone = user.Phone
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? "MySuperSecretKeyForGiupViec3MienApiIsHere!!123";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("Phone", user.Phone)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
