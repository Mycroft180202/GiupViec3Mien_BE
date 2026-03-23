using System;

namespace GiupViec3Mien.Services.DTOs.Matching;

public class EmployerExperienceDto
{
    public Guid EmployerId { get; set; }
    public int TotalJobsPosted { get; set; }
    public int CompletedJobs { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public string TrustLevel { get; set; } = "Medium"; // Low, Medium, High
}
