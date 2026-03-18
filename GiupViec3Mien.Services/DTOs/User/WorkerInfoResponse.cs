using System;
using System.Collections.Generic;

namespace GiupViec3Mien.Services.DTOs.User;

public class WorkerInfoResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int Age { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Worker Profile Specific
    public string? Bio { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public bool Verified { get; set; }
    public List<string> Skills { get; set; } = new();
    
    public DateTime JoinedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
