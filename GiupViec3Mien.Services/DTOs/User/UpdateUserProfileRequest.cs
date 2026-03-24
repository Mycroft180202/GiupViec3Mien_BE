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
    public string? DesiredJobTitle { get; set; }
    public string? SeekingDescription { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsProfilePublic { get; set; }
    public List<string> PreferredLocations { get; set; } = new();
    public List<string> DesiredServiceCategories { get; set; } = new();
    public List<string> Skills { get; set; } = new();

    public string? AdditionalInfo { get; set; }
}
