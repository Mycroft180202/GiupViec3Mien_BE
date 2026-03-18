using System;

namespace GiupViec3Mien.Domain.Entities;

/// <summary>
/// Bản ghi đăng ký khóa học của một người dùng (người giúp việc).
/// </summary>
public class CourseEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }
    public TrainingCourse? Course { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Đã hoàn thành toàn bộ khóa học chưa.</summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>Ngày hoàn thành (nếu có).</summary>
    public DateTime? CompletedAt { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
