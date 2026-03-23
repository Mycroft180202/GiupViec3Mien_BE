using System;

namespace GiupViec3Mien.Services.DTOs.Subscription;

public class SubscriptionStatsResponse
{
    public int TotalPackages { get; set; }
    public int ActivePackages { get; set; }
    public int InactivePackages { get; set; }
    public int TotalPremiumUsers { get; set; }
    public int ActivePremiumUsers { get; set; }
}

public class SubscriptionPackageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    
    // Features
    public bool CanViewApplicantContact { get; set; }
    public int MaxApplicationsView { get; set; }
    public bool PriorityJobPlacement { get; set; }
}

public class SubscriptionPackageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    
    public bool CanViewApplicantContact { get; set; }
    public int MaxApplicationsView { get; set; }
    public bool PriorityJobPlacement { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
