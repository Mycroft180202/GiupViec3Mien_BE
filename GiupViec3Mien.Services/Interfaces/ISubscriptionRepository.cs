using GiupViec3Mien.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Interfaces;

public interface ISubscriptionRepository
{
    Task AddAsync(SubscriptionPackage package);
    Task UpdateAsync(SubscriptionPackage package);
    Task DeleteAsync(SubscriptionPackage package);
    Task<SubscriptionPackage?> GetByIdAsync(Guid id);
    Task<IEnumerable<SubscriptionPackage>> GetAllAsync(bool includeInactive = false);
    Task<int> CountAsync();
    Task<int> CountActiveAsync();
    Task SaveChangesAsync();
}
