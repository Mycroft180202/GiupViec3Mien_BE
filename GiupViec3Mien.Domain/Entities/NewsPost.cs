using GiupViec3Mien.Domain.Enums;
using System;

namespace GiupViec3Mien.Domain.Entities;

/// <summary>
/// Bài đăng trên bảng tin hệ thống (news feed).
/// Admin tạo, mọi người dùng đều có thể xem.
/// </summary>
public class NewsPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tiêu đề bài viết.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Tóm tắt ngắn (dùng trong danh sách / preview).</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Nội dung đầy đủ (hỗ trợ HTML/Markdown từ frontend).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Phân loại bài viết.</summary>
    public NewsFeedCategory Category { get; set; } = NewsFeedCategory.Other;

    /// <summary>URL ảnh thumbnail / cover.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Tags phụ trợ tìm kiếm (JSON array, e.g. ["trẻ em","sơ sinh"]).</summary>
    public string? Tags { get; set; }

    /// <summary>Bài viết có đang hiển thị công khai không.</summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>Số lượt xem.</summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>Admin đã đăng bài.</summary>
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
