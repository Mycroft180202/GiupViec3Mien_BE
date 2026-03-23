using System;

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
    
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public bool IsAccepted { get; set; } = false;
}
