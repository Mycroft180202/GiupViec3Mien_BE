using System;
using GiupViec3Mien.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiupViec3Mien.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsGuest { get; set; } = false;
    public string? AvatarUrl { get; set; }
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? AdditionalInfo { get; set; } // JSONB mapping for dynamic metadata

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public WorkerProfile? WorkerProfile { get; set; }
}
