using System;
using System.Collections.Generic;

namespace GiupViec3Mien.Services.DTOs.User;

public class PublicWorkerCardResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? DesiredJobTitle { get; set; }
    public string? SeekingDescription { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public bool Verified { get; set; }
    public string? LocationSummary { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> DesiredServiceCategories { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class PublicWorkerSearchRequest
{
    public string? Keyword { get; set; }
    public string? Location { get; set; }
    public string? ServiceCategory { get; set; }
}
