using System;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Domain.Entities;

public class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    public Guid ApplicantId { get; set; }
    public User? Applicant { get; set; }

    public string? Message { get; set; }
    public decimal BidPrice { get; set; }
    public string? CvUrl { get; set; }
    public DateTime? AvailableStartDate { get; set; }
    
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public bool IsAccepted { get; set; } = false;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
}
