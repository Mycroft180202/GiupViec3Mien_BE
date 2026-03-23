using GiupViec3Mien.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IJobRepository
{
    Task AddAsync(GiupViec3Mien.Domain.Entities.Job job);
    Task DeleteAsync(GiupViec3Mien.Domain.Entities.Job job);
    Task<GiupViec3Mien.Domain.Entities.Job?> GetByIdAsync(Guid id);
    Task<GiupViec3Mien.Domain.Entities.Job?> GetLatestJobByEmployerAsync(Guid employerId);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetActiveJobsAsync();
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetJobsByPostTypeAsync(GiupViec3Mien.Domain.Enums.PostType postType);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetJobsByEmployerAsync(Guid employerId);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetJobsBySkillsAsync(IEnumerable<string> skills);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetAllAsync();
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> SearchAsync(string? keyword, GiupViec3Mien.Domain.Enums.ServiceCategory? category, string? location, decimal? minPrice, decimal? maxPrice, GiupViec3Mien.Domain.Enums.JobTimingType? timing, GiupViec3Mien.Domain.Enums.PostType postType);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetCreatedSinceAsync(DateTime date);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetByAssignedWorkerIdAsync(Guid workerId);
    Task SaveChangesAsync();
}
