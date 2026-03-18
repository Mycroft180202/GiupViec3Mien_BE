using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly ApplicationDbContext _context;

    public ActivityLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ActivityLog log)
    {
        await _context.ActivityLogs.AddAsync(log);
    }

    public async Task<IEnumerable<ActivityLog>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        return await _context.ActivityLogs
            .Include(a => a.Actor)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetByActorAsync(Guid actorId, int page = 1, int pageSize = 50)
    {
        return await _context.ActivityLogs
            .Include(a => a.Actor)
            .Where(a => a.ActorId == actorId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetByEntityAsync(string entityType, Guid entityId)
    {
        return await _context.ActivityLogs
            .Include(a => a.Actor)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.ActivityLogs.CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
