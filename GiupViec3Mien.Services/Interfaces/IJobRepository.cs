using GiupViec3Mien.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IJobRepository
{
    Task AddAsync(GiupViec3Mien.Domain.Entities.Job job);
    Task<GiupViec3Mien.Domain.Entities.Job?> GetByIdAsync(Guid id);
    Task<GiupViec3Mien.Domain.Entities.Job?> GetLatestJobByEmployerAsync(Guid employerId);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetActiveJobsAsync();
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetJobsByEmployerAsync(Guid employerId);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Job>> GetJobsBySkillsAsync(IEnumerable<string> skills);
    Task SaveChangesAsync();
}
