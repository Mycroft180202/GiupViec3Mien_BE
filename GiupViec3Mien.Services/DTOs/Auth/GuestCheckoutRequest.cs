using System.ComponentModel.DataAnnotations;

namespace GiupViec3Mien.Services.DTOs.Auth;

public class GuestCheckoutRequest
{
    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;
}
