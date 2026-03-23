using GiupViec3Mien.Services.DTOs.Subscription;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Subscription;

public interface ISubscriptionService
{
    Task<SubscriptionPackageResponse> CreatePackageAsync(SubscriptionPackageRequest request);
    Task<SubscriptionPackageResponse?> UpdatePackageAsync(Guid id, SubscriptionPackageRequest request);
    Task<bool> DeletePackageAsync(Guid id);
    Task<IEnumerable<SubscriptionPackageResponse>> GetAllPackagesAsync(bool includeInactive = false);
    Task<SubscriptionPackageResponse?> GetPackageByIdAsync(Guid id);
    
    // User subscription management
    Task<bool> SubscribeUserAsync(Guid userId, Guid packageId);

    // Admin operations
    Task<SubscriptionPackageResponse?> TogglePackageActiveAsync(Guid id);
    Task<SubscriptionStatsResponse> GetSubscriptionStatsAsync();
}
