using System.ComponentModel.DataAnnotations;

namespace GiupViec3Mien.Services.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}
