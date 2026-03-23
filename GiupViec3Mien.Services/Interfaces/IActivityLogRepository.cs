using GiupViec3Mien.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog log);
    Task<IEnumerable<ActivityLog>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<ActivityLog>> GetByActorAsync(Guid actorId, int page = 1, int pageSize = 50);
    Task<IEnumerable<ActivityLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task<int> CountAsync();
    Task SaveChangesAsync();
}
