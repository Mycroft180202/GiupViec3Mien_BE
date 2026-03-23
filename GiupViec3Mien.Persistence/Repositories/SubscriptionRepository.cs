using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SubscriptionPackage package)
    {
        await _context.SubscriptionPackages.AddAsync(package);
    }

    public Task UpdateAsync(SubscriptionPackage package)
    {
        _context.SubscriptionPackages.Update(package);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(SubscriptionPackage package)
    {
        _context.SubscriptionPackages.Remove(package);
        return Task.CompletedTask;
    }

    public async Task<SubscriptionPackage?> GetByIdAsync(Guid id)
    {
        return await _context.SubscriptionPackages.FindAsync(id);
    }

    public async Task<IEnumerable<SubscriptionPackage>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.SubscriptionPackages.AsQueryable();
        if (!includeInactive) query = query.Where(p => p.IsActive);
        return await query.OrderBy(p => p.Price).ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.SubscriptionPackages.CountAsync();
    }

    public async Task<int> CountActiveAsync()
    {
        return await _context.SubscriptionPackages.CountAsync(p => p.IsActive);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
