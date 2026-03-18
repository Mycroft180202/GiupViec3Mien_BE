using System;

namespace GiupViec3Mien.Domain.Entities;

/// <summary>
/// Tracks admin and system-level actions for auditing purposes.
/// </summary>
public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The admin or system user who performed the action. Null = system-initiated.</summary>
    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }

    /// <summary>Short action code, e.g. "LockUser", "DeleteJob", "CreatePackage".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Resource type affected, e.g. "User", "Job", "Package".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>The Id of the affected entity (if applicable).</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Human-readable description of what happened.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional JSON blob for extra context (before/after values, etc.).</summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
