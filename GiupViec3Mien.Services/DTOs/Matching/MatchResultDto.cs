using System;

namespace GiupViec3Mien.Services.DTOs.Matching;

public class MatchResultDto
{
    public Guid WorkerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public double MatchScore { get; set; }
    public double DistanceKm { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public bool Verified { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
}
