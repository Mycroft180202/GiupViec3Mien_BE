using GiupViec3Mien.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GiupViec3Mien.Services.DTOs.NewsFeed;

// ─────────────────────────────── Requests ────────────────────────────────

public class CreateNewsPostRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public NewsFeedCategory Category { get; set; } = NewsFeedCategory.Other;

    public string? ThumbnailUrl { get; set; }

    /// <summary>Danh sách tag (sẽ được serialize thành JSON).</summary>
    public List<string>? Tags { get; set; }

    /// <summary>true = xuất bản ngay, false = lưu nháp.</summary>
    public bool IsPublished { get; set; } = false;
}

public class UpdateNewsPostRequest
{
    [MaxLength(300)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Summary { get; set; }

    public string? Content { get; set; }
    public NewsFeedCategory? Category { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsPublished { get; set; }
}

// ─────────────────────────────── Responses ───────────────────────────────

public class NewsPostSummaryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public NewsFeedCategory Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class NewsPostDetailResponse : NewsPostSummaryResponse
{
    public string Content { get; set; } = string.Empty;
}

public class NewsPostPagedResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<NewsPostSummaryResponse> Data { get; set; } = new List<NewsPostSummaryResponse>();
}
