using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.DTOs.Subscription;
using GiupViec3Mien.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;

    public SubscriptionService(ISubscriptionRepository subscriptionRepository, IUserRepository userRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubscriptionPackageResponse> CreatePackageAsync(SubscriptionPackageRequest request)
    {
        var package = new SubscriptionPackage
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            DurationDays = request.DurationDays,
            CanViewApplicantContact = request.CanViewApplicantContact,
            MaxApplicationsView = request.MaxApplicationsView,
            PriorityJobPlacement = request.PriorityJobPlacement,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _subscriptionRepository.AddAsync(package);
        await _subscriptionRepository.SaveChangesAsync();

        return MapToResponse(package);
    }

    public async Task<SubscriptionPackageResponse?> UpdatePackageAsync(Guid id, SubscriptionPackageRequest request)
    {
        var package = await _subscriptionRepository.GetByIdAsync(id);
        if (package == null) return null;

        package.Name = request.Name;
        package.Description = request.Description;
        package.Price = request.Price;
        package.DurationDays = request.DurationDays;
        package.CanViewApplicantContact = request.CanViewApplicantContact;
        package.MaxApplicationsView = request.MaxApplicationsView;
        package.PriorityJobPlacement = request.PriorityJobPlacement;
        package.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(package);
        await _subscriptionRepository.SaveChangesAsync();

        return MapToResponse(package);
    }

    public async Task<bool> DeletePackageAsync(Guid id)
    {
        var package = await _subscriptionRepository.GetByIdAsync(id);
        if (package == null) return false;

        await _subscriptionRepository.DeleteAsync(package);
        await _subscriptionRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SubscriptionPackageResponse>> GetAllPackagesAsync(bool includeInactive = false)
    {
        var packages = await _subscriptionRepository.GetAllAsync(includeInactive);
        return packages.Select(MapToResponse);
    }

    public async Task<SubscriptionPackageResponse?> GetPackageByIdAsync(Guid id)
    {
        var package = await _subscriptionRepository.GetByIdAsync(id);
        return package != null ? MapToResponse(package) : null;
    }

    public async Task<bool> SubscribeUserAsync(Guid userId, Guid packageId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var package = await _subscriptionRepository.GetByIdAsync(packageId);

        if (user == null || package == null || !package.IsActive) return false;

        user.HasPremiumAccess = true;
        user.PremiumExpiry = DateTime.UtcNow.AddDays(package.DurationDays);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();
        return true;
    }

    public async Task<SubscriptionPackageResponse?> TogglePackageActiveAsync(Guid id)
    {
        var package = await _subscriptionRepository.GetByIdAsync(id);
        if (package == null) return null;

        package.IsActive = !package.IsActive;
        package.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(package);
        await _subscriptionRepository.SaveChangesAsync();

        return MapToResponse(package);
    }

    public async Task<SubscriptionStatsResponse> GetSubscriptionStatsAsync()
    {
        var totalPackages = await _subscriptionRepository.CountAsync();
        var activePackages = await _subscriptionRepository.CountActiveAsync();
        var allUsers = await _userRepository.GetAllAsync();

        var now = DateTime.UtcNow;
        var userList = allUsers.ToList();

        return new SubscriptionStatsResponse
        {
            TotalPackages = totalPackages,
            ActivePackages = activePackages,
            InactivePackages = totalPackages - activePackages,
            TotalPremiumUsers = userList.Count(u => u.HasPremiumAccess),
            ActivePremiumUsers = userList.Count(u => u.HasPremiumAccess && u.PremiumExpiry > now)
        };
    }

    private static SubscriptionPackageResponse MapToResponse(SubscriptionPackage p)
    {
        return new SubscriptionPackageResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            DurationDays = p.DurationDays,
            CanViewApplicantContact = p.CanViewApplicantContact,
            MaxApplicationsView = p.MaxApplicationsView,
            PriorityJobPlacement = p.PriorityJobPlacement,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }
}
