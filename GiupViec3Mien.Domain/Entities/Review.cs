using System;

namespace GiupViec3Mien.Domain.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    public Guid ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    public Guid RevieweeId { get; set; }
    public User? Reviewee { get; set; }

    public int Rating { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
