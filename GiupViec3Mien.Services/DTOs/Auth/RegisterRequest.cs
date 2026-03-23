using System.ComponentModel.DataAnnotations;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    public Role Role { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public GenderOption Gender { get; set; } = GenderOption.Any;

    public DateTime? DateOfBirth { get; set; }
}
