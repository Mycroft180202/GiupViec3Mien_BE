using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
