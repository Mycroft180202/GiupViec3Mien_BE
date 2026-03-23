using System;
using System.Collections.Generic;

namespace GiupViec3Mien.Services.DTOs.Matching;

public class EmployerMatchResultDto
{
    public Guid EmployerId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string EmployerName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public double MatchScore { get; set; }
    public double DistanceKm { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TotalJobsPosted { get; set; }
    public int CompletedJobs { get; set; }
    public string TrustLevel { get; set; } = "Medium";
    public bool IsVerified { get; set; }
    public double BudgetFitScore { get; set; }
    public double FeedbackScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
}
