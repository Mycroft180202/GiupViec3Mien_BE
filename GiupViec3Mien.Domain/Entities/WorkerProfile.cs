using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiupViec3Mien.Domain.Entities;

public class WorkerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? Bio { get; set; }
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public bool Verified { get; set; } = false;

    [Column(TypeName = "jsonb")]
    public string? Skills { get; set; } // JSON array of skills: e.g. ["Nấu ăn", "Dọn dẹp"]

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
