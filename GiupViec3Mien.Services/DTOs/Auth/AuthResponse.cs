using GiupViec3Mien.Domain.Enums;
using System;

namespace GiupViec3Mien.Services.DTOs.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public bool IsGuest { get; set; }
    public bool HasPremiumAccess { get; set; }
    public DateTime? PremiumExpiry { get; set; }
}
