using GiupViec3Mien.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetByRevieweeIdAsync(Guid revieweeId);
    Task<Review?> GetReviewAsync(Guid jobId, Guid reviewerId, Guid revieweeId);
}
