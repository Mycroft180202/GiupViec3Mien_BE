using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.NewsFeed;
using GiupViec3Mien.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.NewsFeed;

public class NewsService : INewsService
{
    private readonly INewsPostRepository _newsRepo;

    public NewsService(INewsPostRepository newsRepo)
    {
        _newsRepo = newsRepo;
    }

    // ── Public ────────────────────────────────────────────────────────────

    public async Task<NewsPostPagedResponse> GetPublishedPostsAsync(
        int page = 1,
        int pageSize = 20,
        NewsFeedCategory? category = null,
        string? searchTerm = null)
    {
        var posts = await _newsRepo.GetPublishedAsync(page, pageSize, category, searchTerm);
        var total = await _newsRepo.CountPublishedAsync(category);

        return new NewsPostPagedResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = posts.Select(MapToSummary)
        };
    }

    public async Task<NewsPostDetailResponse?> GetPostDetailAsync(Guid id)
    {
        var post = await _newsRepo.GetByIdAsync(id);
        if (post == null || !post.IsPublished) return null;

        // Fire-and-forget view count (best effort)
        _ = _newsRepo.IncrementViewCountAsync(id);

        return MapToDetail(post);
    }

    // ── Admin ─────────────────────────────────────────────────────────────

    public async Task<NewsPostPagedResponse> GetAllPostsAsync(int page = 1, int pageSize = 20)
    {
        var posts = await _newsRepo.GetAllAsync(page, pageSize);
        var total = await _newsRepo.CountAllAsync();

        return new NewsPostPagedResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = posts.Select(MapToSummary)
        };
    }

    public async Task<NewsPostDetailResponse> CreatePostAsync(Guid authorId, CreateNewsPostRequest request)
    {
        var post = new NewsPost
        {
            Title = request.Title,
            Summary = request.Summary,
            Content = request.Content,
            Category = request.Category,
            ThumbnailUrl = request.ThumbnailUrl,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            IsPublished = request.IsPublished,
            AuthorId = authorId,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null
        };

        await _newsRepo.AddAsync(post);
        await _newsRepo.SaveChangesAsync();

        // Reload with Author navigation
        var saved = await _newsRepo.GetByIdAsync(post.Id);
        return MapToDetail(saved!);
    }

    public async Task<NewsPostDetailResponse?> UpdatePostAsync(Guid id, UpdateNewsPostRequest request)
    {
        var post = await _newsRepo.GetByIdAsync(id);
        if (post == null) return null;

        if (request.Title != null) post.Title = request.Title;
        if (request.Summary != null) post.Summary = request.Summary;
        if (request.Content != null) post.Content = request.Content;
        if (request.Category.HasValue) post.Category = request.Category.Value;
        if (request.ThumbnailUrl != null) post.ThumbnailUrl = request.ThumbnailUrl;
        if (request.Tags != null) post.Tags = JsonSerializer.Serialize(request.Tags);

        if (request.IsPublished.HasValue)
        {
            var wasPublished = post.IsPublished;
            post.IsPublished = request.IsPublished.Value;
            if (!wasPublished && post.IsPublished)
                post.PublishedAt = DateTime.UtcNow;
            else if (!post.IsPublished)
                post.PublishedAt = null;
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _newsRepo.SaveChangesAsync();

        return MapToDetail(post);
    }

    public async Task<bool> DeletePostAsync(Guid id)
    {
        var post = await _newsRepo.GetByIdAsync(id);
        if (post == null) return false;

        await _newsRepo.DeleteAsync(post);
        await _newsRepo.SaveChangesAsync();
        return true;
    }

    public async Task<NewsPostDetailResponse?> PublishPostAsync(Guid id)
    {
        var post = await _newsRepo.GetByIdAsync(id);
        if (post == null) return null;

        post.IsPublished = true;
        post.PublishedAt ??= DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        await _newsRepo.SaveChangesAsync();
        return MapToDetail(post);
    }

    public async Task<NewsPostDetailResponse?> UnpublishPostAsync(Guid id)
    {
        var post = await _newsRepo.GetByIdAsync(id);
        if (post == null) return null;

        post.IsPublished = false;
        post.UpdatedAt = DateTime.UtcNow;

        await _newsRepo.SaveChangesAsync();
        return MapToDetail(post);
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static NewsPostSummaryResponse MapToSummary(NewsPost post) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Summary = post.Summary,
        Category = post.Category,
        CategoryLabel = GetCategoryLabel(post.Category),
        ThumbnailUrl = post.ThumbnailUrl,
        Tags = DeserializeTags(post.Tags),
        IsPublished = post.IsPublished,
        ViewCount = post.ViewCount,
        AuthorId = post.AuthorId,
        AuthorName = post.Author?.FullName ?? "Admin",
        PublishedAt = post.PublishedAt,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt
    };

    private static NewsPostDetailResponse MapToDetail(NewsPost post) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Summary = post.Summary,
        Content = post.Content,
        Category = post.Category,
        CategoryLabel = GetCategoryLabel(post.Category),
        ThumbnailUrl = post.ThumbnailUrl,
        Tags = DeserializeTags(post.Tags),
        IsPublished = post.IsPublished,
        ViewCount = post.ViewCount,
        AuthorId = post.AuthorId,
        AuthorName = post.Author?.FullName ?? "Admin",
        PublishedAt = post.PublishedAt,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt
    };

    private static List<string> DeserializeTags(string? tagsJson)
    {
        if (string.IsNullOrEmpty(tagsJson)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    private static string GetCategoryLabel(NewsFeedCategory category) => category switch
    {
        NewsFeedCategory.HousekeepingTips        => "Kinh nghiệm giúp việc",
        NewsFeedCategory.BabysittingTips         => "Mẹo chăm sóc trẻ",
        NewsFeedCategory.ElderCareTips           => "Mẹo chăm sóc người già",
        NewsFeedCategory.RecruitmentAnnouncement => "Thông báo tuyển dụng",
        NewsFeedCategory.SystemAnnouncement      => "Thông báo hệ thống",
        _                                        => "Khác"
    };
}
