using GiupViec3Mien.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IJobRepository
{
    Task AddAsync(Domain.Entities.Job job);
    Task<Domain.Entities.Job?> GetByIdAsync(System.Guid id);
    Task<Domain.Entities.Job?> GetLatestJobByEmployerAsync(Guid employerId);
    Task<IEnumerable<Domain.Entities.Job>> GetActiveJobsAsync();
    Task SaveChangesAsync();
}
