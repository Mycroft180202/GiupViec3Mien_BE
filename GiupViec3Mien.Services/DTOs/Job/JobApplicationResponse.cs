using System;

namespace GiupViec3Mien.Services.DTOs.Job;

public class JobApplicationResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public decimal JobPrice { get; set; }
    public Guid ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public decimal BidPrice { get; set; }
    public string? CvUrl { get; set; }
    public DateTime AppliedAt { get; set; }
    public bool IsAccepted { get; set; }
}
