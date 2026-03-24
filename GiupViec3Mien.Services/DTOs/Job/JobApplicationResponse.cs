using System;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Job;

public class JobApplicationResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public decimal JobPrice { get; set; }
    public Guid ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantAvatarUrl { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? ApplicantEmail { get; set; }
    public string? Message { get; set; }
    public decimal BidPrice { get; set; }
    public string? CvUrl { get; set; }
    public DateTime? AvailableStartDate { get; set; }
    public DateTime AppliedAt { get; set; }
    public bool IsAccepted { get; set; }
    public ApplicationStatus Status { get; set; }
    public string CompanyHotline { get; set; } = "1900-xxxx (Giúp Việc 3 Miền Support)";
}
