using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

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

    public async Task DeleteAsync(Job job)
    {
        _context.Jobs.Remove(job);
    }

    public async Task<Job?> GetByIdAsync(Guid id)
    {
        return await _context.Jobs.Include(j => j.Employer).FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<Job?> GetLatestJobByEmployerAsync(Guid employerId)
    {
        return await _context.Jobs
            .Where(j => j.EmployerId == employerId && j.Status == Domain.Enums.JobStatus.Open)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Job>> GetActiveJobsAsync()
    {
        return await _context.Jobs
            .Include(j => j.Employer)
            .Where(j => j.Status == Domain.Enums.JobStatus.Open && j.PostType == Domain.Enums.PostType.Hiring)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Job>> GetJobsByPostTypeAsync(GiupViec3Mien.Domain.Enums.PostType postType)
    {
        return await _context.Jobs
            .Include(j => j.Employer)
            .Where(j => j.PostType == postType && j.Status == Domain.Enums.JobStatus.Open)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Job>> GetJobsByEmployerAsync(Guid employerId)
    {
        return await _context.Jobs
            .Include(j => j.AssignedWorker)
            .Include(j => j.Applications)
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Job>> GetJobsBySkillsAsync(IEnumerable<string> skills)
    {
        var query = _context.Jobs
            .Include(j => j.Employer)
            .Where(j => j.Status == Domain.Enums.JobStatus.Open);

        var skillList = skills.ToList();
        if (!skillList.Any()) return await query.ToListAsync();

        // In PostgreSQL, for a JSONB array, we can use the ?| operator (any of these exist)
        // Since we are using EF Core, we can use EF.Functions or a raw SQL filter if needed.
        // For now, let's use a server-side string check that works with JSON array format:
        // WHERE "RequiredSkills" LIKE '%"skill1"%' OR "RequiredSkills" LIKE '%"skill2"%'
        
        var results = await query.ToListAsync();
        
        // Final filter in memory to ensure correct JSON matching (since LIKE is risky with partial words)
        return results.Where(j => {
            if (string.IsNullOrEmpty(j.RequiredSkills)) return false;
            try {
                var jobSkills = JsonSerializer.Deserialize<List<string>>(j.RequiredSkills);
                return jobSkills != null && jobSkills.Any(s => skillList.Contains(s, StringComparer.OrdinalIgnoreCase));
            } catch { return false; }
        });
    }

    public async Task<IEnumerable<Job>> GetAllAsync()
    {
        return await _context.Jobs
            .Include(j => j.Employer)
            .Include(j => j.Applications)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
