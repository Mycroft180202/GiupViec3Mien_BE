using System;

namespace GiupViec3Mien.Domain.Entities;

/// <summary>
/// Một bài học (lesson) bên trong khóa đào tạo.
/// </summary>
public class CourseLesson
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }
    public TrainingCourse? Course { get; set; }

    /// <summary>Tiêu đề bài học.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Nội dung / mô tả bài học (HTML/Markdown).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Thứ tự hiển thị trong khóa học.</summary>
    public int Order { get; set; } = 0;

    /// <summary>Thời lượng bài học (phút).</summary>
    public int DurationMinutes { get; set; } = 0;

    /// <summary>URL video bài học (YouTube, Vimeo, …).</summary>
    public string? VideoUrl { get; set; }

    /// <summary>URL tài liệu đính kèm (PDF, DOCX, …).</summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>Bài học preview – cho phép xem miễn phí dù khóa có phí.</summary>
    public bool IsPreview { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
