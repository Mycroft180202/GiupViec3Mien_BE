using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Review>> GetByRevieweeIdAsync(Guid revieweeId)
    {
        return await _context.Reviews
            .Where(r => r.RevieweeId == revieweeId)
            .ToListAsync();
    }

    public async Task<Review?> GetReviewAsync(Guid jobId, Guid reviewerId, Guid revieweeId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.JobId == jobId && r.ReviewerId == reviewerId && r.RevieweeId == revieweeId);
    }
}
