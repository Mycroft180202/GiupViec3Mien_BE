using System;
using GiupViec3Mien.Domain.Enums;

namespace GiupViec3Mien.Services.DTOs.Admin;

// ──────────────────────────── User management ────────────────────────────

public class AdminUserDetailResponse
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsGuest { get; set; }
    public bool IsLocked { get; set; }
    public bool HasPremiumAccess { get; set; }
    public DateTime? PremiumExpiry { get; set; }
    public string? AvatarUrl { get; set; }
    public bool PhoneVerified { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    public string? PhoneVerificationChannel { get; set; }
    public GenderOption Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? AdditionalInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Worker-specific (null for non-workers)
    public WorkerProfileSummary? WorkerProfile { get; set; }
}

public class WorkerProfileSummary
{
    public string? Bio { get; set; }
    public string? DesiredJobTitle { get; set; }
    public string? SeekingDescription { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public bool Verified { get; set; }
    public bool IsProfilePublic { get; set; }
    public string? Skills { get; set; }
    public string? PreferredLocations { get; set; }
    public string? DesiredServiceCategories { get; set; }
}

public class AdminUpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Role? Role { get; set; }
    public bool? HasPremiumAccess { get; set; }
    public DateTime? PremiumExpiry { get; set; }
    public bool? IsLocked { get; set; }
}

// ──────────────────────────── Activity log ────────────────────────────

public class ActivityLogResponse
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ──────────────────────────── System stats ────────────────────────────

public class AdminSystemStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalWorkers { get; set; }
    public int TotalEmployers { get; set; }
    public int LockedAccounts { get; set; }
    public int ActivePremiumUsers { get; set; }
    public int TotalJobs { get; set; }
    public int OpenJobs { get; set; }
    public int InProgressJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int TotalApplications { get; set; }
    public int TotalPackages { get; set; }
    public int ActivePackages { get; set; }
    public bool SystemHealthy { get; set; }
    public DateTime LastUpdate { get; set; }
}
