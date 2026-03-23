using System;

namespace GiupViec3Mien.Services.DTOs.Matching;

public class MatchResultDto
{
    public Guid WorkerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public double MatchScore { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public double DistanceKm { get; set; }
    public double AverageRating { get; set; }
    public int TotalJobsPosted { get; set; }
    public int CompletedJobs { get; set; }
    public string TrustLevel { get; set; } = "Medium";
    public bool IsVerified { get; set; }
    public bool Verified { get; set; }
    public int ReviewCount { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public double BudgetFitScore { get; set; }
    public double FeedbackScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
}
