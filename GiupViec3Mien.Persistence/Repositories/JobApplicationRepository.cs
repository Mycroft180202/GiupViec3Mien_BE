using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public JobApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(JobApplication application)
    {
        await _context.JobApplications.AddAsync(application);
    }

    public async Task<JobApplication?> GetByIdAsync(Guid id)
    {
        return await _context.JobApplications
            .Include(ja => ja.Job)
            .Include(ja => ja.Applicant)
            .FirstOrDefaultAsync(ja => ja.Id == id);
    }

    public async Task<IEnumerable<JobApplication>> GetByJobIdAsync(Guid jobId)
    {
        return await _context.JobApplications
            .Include(ja => ja.Applicant)
            .Where(ja => ja.JobId == jobId)
            .OrderByDescending(ja => ja.AppliedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobApplication>> GetByApplicantIdAsync(Guid applicantId)
    {
        return await _context.JobApplications
            .Include(ja => ja.Job)
            .Where(ja => ja.ApplicantId == applicantId)
            .OrderByDescending(ja => ja.AppliedAt)
            .ToListAsync();
    }

    public async Task<JobApplication?> GetByApplicantAndJobAsync(Guid applicantId, Guid jobId)
    {
        return await _context.JobApplications
            .Include(ja => ja.Job)
            .FirstOrDefaultAsync(ja => ja.ApplicantId == applicantId && ja.JobId == jobId);
    }

    public async Task<int> CountAsync()
    {
        return await _context.JobApplications.CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
