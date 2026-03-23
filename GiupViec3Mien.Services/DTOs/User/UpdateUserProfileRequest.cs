using System;
using System.Collections.Generic;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.User;

public class UpdateUserProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public GenderOption Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? AvatarUrl { get; set; }
    
    // For Workers
    public string? Bio { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }

    public string? AdditionalInfo { get; set; }
}
