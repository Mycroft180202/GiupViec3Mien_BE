using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _context;

    public JobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Job job)
    {
        await _context.Jobs.AddAsync(job);
    }

    public async Task<Job?> GetByIdAsync(Guid id)
    {
        return await _context.Jobs.Include(j => j.Employer).FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IEnumerable<Job>> GetActiveJobsAsync()
    {
        return await _context.Jobs
            .Include(j => j.Employer)
            .Where(j => j.Status == Domain.Enums.JobStatus.Open)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
