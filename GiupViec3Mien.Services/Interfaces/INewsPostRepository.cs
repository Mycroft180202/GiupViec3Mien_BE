using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface INewsPostRepository
{
    Task AddAsync(NewsPost post);
    Task<NewsPost?> GetByIdAsync(Guid id);

    /// <summary>Danh sách bài viết đã xuất bản (public feed), có phân trang.</summary>
    Task<IEnumerable<NewsPost>> GetPublishedAsync(
        int page = 1,
        int pageSize = 20,
        NewsFeedCategory? category = null,
        string? searchTerm = null);

    /// <summary>Tất cả bài viết (kể cả chưa xuất bản) – dành cho admin.</summary>
    Task<IEnumerable<NewsPost>> GetAllAsync(int page = 1, int pageSize = 20);

    Task<int> CountPublishedAsync(NewsFeedCategory? category = null);
    Task<int> CountAllAsync();

    Task DeleteAsync(NewsPost post);
    Task IncrementViewCountAsync(Guid id);
    Task SaveChangesAsync();
}
