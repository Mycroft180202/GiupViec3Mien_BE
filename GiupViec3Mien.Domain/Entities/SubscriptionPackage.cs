using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiupViec3Mien.Domain.Entities;

public class SubscriptionPackage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    
    // Features
    public bool CanViewApplicantContact { get; set; } = false;
    public int MaxApplicationsView { get; set; } = 5; // Default limit for free users might be 5, packages can increase this
    public bool PriorityJobPlacement { get; set; } = false;
    
    [Column(TypeName = "jsonb")]
    public string? AdditionalBenefits { get; set; } // For future-proofing

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
