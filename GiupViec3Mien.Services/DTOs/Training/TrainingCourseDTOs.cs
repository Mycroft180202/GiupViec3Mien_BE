using GiupViec3Mien.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GiupViec3Mien.Services.DTOs.Training;

// ─────────────────────────── Course Requests ─────────────────────────────

public class CreateCourseRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public CourseCategory Category { get; set; } = CourseCategory.Other;
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;

    public string? ThumbnailUrl { get; set; }

    /// <summary>Tổng thời lượng (phút). Có thể cập nhật tự động từ tổng bài học.</summary>
    public int DurationMinutes { get; set; } = 0;

    /// <summary>Để null = miễn phí.</summary>
    public decimal? Price { get; set; }

    public bool RequiresPremium { get; set; } = false;

    /// <summary>true = xuất bản ngay.</summary>
    public bool IsPublished { get; set; } = false;
}

public class UpdateCourseRequest
{
    [MaxLength(300)]
    public string? Title { get; set; }
    public string? Description { get; set; }
    public CourseCategory? Category { get; set; }
    public CourseLevel? Level { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? Price { get; set; }
    public bool? RequiresPremium { get; set; }
    public bool? IsPublished { get; set; }
}

// ─────────────────────────── Lesson Requests ─────────────────────────────

public class CreateLessonRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>Thứ tự trong khóa học (0-indexed hoặc tự động thêm vào cuối nếu bỏ trống).</summary>
    public int? Order { get; set; }

    public int DurationMinutes { get; set; } = 0;
    public string? VideoUrl { get; set; }
    public string? AttachmentUrl { get; set; }

    /// <summary>Bài học preview – có thể xem miễn phí.</summary>
    public bool IsPreview { get; set; } = false;
}

public class UpdateLessonRequest
{
    [MaxLength(300)]
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? Order { get; set; }
    public int? DurationMinutes { get; set; }
    public string? VideoUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool? IsPreview { get; set; }
}

// ─────────────────────────── Course Responses ────────────────────────────

public class CourseSummaryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CourseCategory Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public CourseLevel Level { get; set; }
    public string LevelLabel { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int DurationMinutes { get; set; }
    public decimal? Price { get; set; }
    public bool RequiresPremium { get; set; }
    public bool IsPublished { get; set; }
    public int LessonCount { get; set; }
    public int EnrollmentCount { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CourseDetailResponse : CourseSummaryResponse
{
    public IEnumerable<LessonResponse> Lessons { get; set; } = new List<LessonResponse>();

    /// <summary>Trạng thái đăng ký của người dùng hiện tại (null nếu chưa đăng nhập).</summary>
    public EnrollmentStatusResponse? MyEnrollment { get; set; }
}

public class CoursePagedResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<CourseSummaryResponse> Data { get; set; } = new List<CourseSummaryResponse>();
}

// ─────────────────────────── Lesson Responses ────────────────────────────

public class LessonResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Nội dung chỉ trả về nếu người dùng có quyền (đã đăng ký / lesson là preview).</summary>
    public string? Content { get; set; }
    public int Order { get; set; }
    public int DurationMinutes { get; set; }
    public string? VideoUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IsPreview { get; set; }

    /// <summary>Người dùng hiện tại có thể xem nội dung không.</summary>
    public bool CanView { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ─────────────────────────── Enrollment Responses ────────────────────────

public class EnrollmentStatusResponse
{
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime EnrolledAt { get; set; }
}

public class MyCourseResponse : CourseSummaryResponse
{
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime EnrolledAt { get; set; }
}
