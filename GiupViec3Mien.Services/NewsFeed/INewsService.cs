using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.NewsFeed;
using System;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.NewsFeed;

public interface INewsService
{
    // ── Public ────────────────────────────────────────────────────────────
    Task<NewsPostPagedResponse> GetPublishedPostsAsync(
        int page = 1,
        int pageSize = 20,
        NewsFeedCategory? category = null,
        string? searchTerm = null);

    Task<NewsPostDetailResponse?> GetPostDetailAsync(Guid id);

    // ── Admin ─────────────────────────────────────────────────────────────
    Task<NewsPostPagedResponse> GetAllPostsAsync(int page = 1, int pageSize = 20);
    Task<NewsPostDetailResponse> CreatePostAsync(Guid authorId, CreateNewsPostRequest request);
    Task<NewsPostDetailResponse?> UpdatePostAsync(Guid id, UpdateNewsPostRequest request);
    Task<bool> DeletePostAsync(Guid id);

    /// <summary>Xuất bản bài viết nháp.</summary>
    Task<NewsPostDetailResponse?> PublishPostAsync(Guid id);

    /// <summary>Hủy xuất bản (chuyển về nháp).</summary>
    Task<NewsPostDetailResponse?> UnpublishPostAsync(Guid id);
}
