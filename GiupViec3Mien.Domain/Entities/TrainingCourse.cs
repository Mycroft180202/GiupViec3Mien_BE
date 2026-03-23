using GiupViec3Mien.Domain.Enums;
using System;
using System.Collections.Generic;

namespace GiupViec3Mien.Domain.Entities;

/// <summary>
/// Khóa đào tạo kỹ năng dành cho người giúp việc.
/// Admin tạo và quản lý.
/// </summary>
public class TrainingCourse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tên khóa học.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Mô tả tổng quan khóa học.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Chủ đề / lĩnh vực của khóa học.</summary>
    public CourseCategory Category { get; set; } = CourseCategory.Other;

    /// <summary>Cấp độ học viên phù hợp.</summary>
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;

    /// <summary>URL ảnh thumbnail / cover khóa học.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Tổng thời lượng (phút).</summary>
    public int DurationMinutes { get; set; } = 0;

    /// <summary>Khóa học có phí hay miễn phí. Null = miễn phí.</summary>
    public decimal? Price { get; set; }

    /// <summary>Yêu cầu tài khoản Premium để xem toàn bộ nội dung.</summary>
    public bool RequiresPremium { get; set; } = false;

    /// <summary>Khóa học đang mở đăng ký / hiển thị công khai.</summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>Số học viên đã đăng ký.</summary>
    public int EnrollmentCount { get; set; } = 0;

    /// <summary>Admin tạo khóa học.</summary>
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    /// <summary>Danh sách bài học trong khóa.</summary>
    public ICollection<CourseLesson> Lessons { get; set; } = new List<CourseLesson>();

    /// <summary>Danh sách học viên đăng ký.</summary>
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
