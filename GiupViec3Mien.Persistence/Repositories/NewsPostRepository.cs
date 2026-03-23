using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class NewsPostRepository : INewsPostRepository
{
    private readonly ApplicationDbContext _context;

    public NewsPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NewsPost post)
    {
        await _context.NewsPosts.AddAsync(post);
    }

    public async Task<NewsPost?> GetByIdAsync(Guid id)
    {
        return await _context.NewsPosts
            .Include(n => n.Author)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<NewsPost>> GetPublishedAsync(
        int page = 1,
        int pageSize = 20,
        NewsFeedCategory? category = null,
        string? searchTerm = null)
    {
        var query = _context.NewsPosts
            .Include(n => n.Author)
            .Where(n => n.IsPublished);

        if (category.HasValue)
            query = query.Where(n => n.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(term) ||
                n.Summary.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<NewsPost>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.NewsPosts
            .Include(n => n.Author)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountPublishedAsync(NewsFeedCategory? category = null)
    {
        var query = _context.NewsPosts.Where(n => n.IsPublished);
        if (category.HasValue) query = query.Where(n => n.Category == category.Value);
        return await query.CountAsync();
    }

    public async Task<int> CountAllAsync()
    {
        return await _context.NewsPosts.CountAsync();
    }

    public Task DeleteAsync(NewsPost post)
    {
        _context.NewsPosts.Remove(post);
        return Task.CompletedTask;
    }

    public async Task IncrementViewCountAsync(Guid id)
    {
        await _context.NewsPosts
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ViewCount, n => n.ViewCount + 1));
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
