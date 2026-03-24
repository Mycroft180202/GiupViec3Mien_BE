using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.DTOs.Admin;
using GiupViec3Mien.Services.DTOs.User;
using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace GiupViec3Mien.Services.UserServices;

public class UserService : IUserService
{
    private const string PublicWorkerVersionCacheKey = "workers:public:version";
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDistributedCache _distributedCache;

    public UserService(
        IUserRepository userRepository,
        IFileStorageService fileStorageService,
        IDistributedCache distributedCache)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _distributedCache = distributedCache;
    }

    public async Task<string> UploadProfileImageAsync(Guid userId, IFormFile file)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var folderName = $"{datePath}/{userId}";

        var imageUrl = await _fileStorageService.UploadImageAsync(file, folderName);

        if (!string.IsNullOrEmpty(imageUrl))
        {
            user.AvatarUrl = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();
            await TouchWorkerCacheVersionAsync();
        }

        return imageUrl;
    }

    public async Task UpdateProfileImageUrlAsync(Guid userId, string imgUrl)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.AvatarUrl = imgUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
        await TouchWorkerCacheVersionAsync();
    }

    public async Task<AdminUserDetailResponse?> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;
        return MapToAdminDetail(user);
    }

    public async Task UpdateSkillsAsync(Guid userId, List<string> skills)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        if (user.Role == Domain.Enums.Role.Worker)
        {
            user.WorkerProfile ??= new WorkerProfile { UserId = userId };
            user.WorkerProfile.Skills = JsonSerializer.Serialize(skills);
            user.WorkerProfile.UpdatedAt = DateTime.UtcNow;
            await TouchWorkerCacheVersionAsync();
        }
        else
        {
            var info = string.IsNullOrEmpty(user.AdditionalInfo)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(user.AdditionalInfo) ?? new Dictionary<string, object>();

            info["skills"] = skills;
            user.AdditionalInfo = JsonSerializer.Serialize(info);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task UpdateEmailAsync(Guid userId, string email)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task<WorkerInfoResponse?> GetWorkerInfoAsync(Guid workerId, Guid requesterId)
    {
        var user = await _userRepository.GetByIdAsync(workerId);
        if (user == null || user.Role != Domain.Enums.Role.Worker)
        {
            return null;
        }

        var profile = user.WorkerProfile;
        var requester = requesterId != Guid.Empty ? await _userRepository.GetByIdAsync(requesterId) : null;
        var isOwner = workerId == requesterId;
        var isAdmin = requester?.Role == Domain.Enums.Role.Admin;
        var isPublicProfile = profile?.IsProfilePublic ?? false;

        if (!isOwner && !isAdmin && !isPublicProfile)
        {
            return null;
        }

        var hasFullAccess = isOwner ||
                            isAdmin ||
                            (requester != null &&
                             requester.HasPremiumAccess &&
                             requester.PremiumExpiry >= DateTime.UtcNow);

        return MapToWorkerInfoResponse(user, hasFullAccess);
    }

    public async Task<IEnumerable<PublicWorkerCardResponse>> GetPublicWorkersAsync(PublicWorkerSearchRequest request)
    {
        var version = await GetWorkerCacheVersionAsync();
        var cacheKey =
            $"workers:public:list:v{version}:keyword={NormalizeKeyPart(request.Keyword)}:location={NormalizeKeyPart(request.Location)}:service={NormalizeKeyPart(request.ServiceCategory)}";

        var cached = await _distributedCache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return JsonSerializer.Deserialize<List<PublicWorkerCardResponse>>(cached) ?? new List<PublicWorkerCardResponse>();
        }

        var workers = await _userRepository.GetPublicWorkersAsync();
        var keyword = request.Keyword?.Trim().ToLowerInvariant();
        var location = request.Location?.Trim().ToLowerInvariant();
        var serviceCategory = request.ServiceCategory?.Trim().ToLowerInvariant();

        var filtered = workers.Where(user =>
        {
            var profile = user.WorkerProfile;
            if (profile == null || !profile.IsProfilePublic)
            {
                return false;
            }

            var skills = DeserializeList(profile.Skills);
            var locations = DeserializeList(profile.PreferredLocations);
            var serviceCategories = DeserializeList(profile.DesiredServiceCategories);

            var keywordMatched = string.IsNullOrWhiteSpace(keyword) ||
                                 user.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                 (profile.DesiredJobTitle?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                 (profile.SeekingDescription?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                 skills.Any(skill => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            var locationMatched = string.IsNullOrWhiteSpace(location) ||
                                  locations.Any(item => item.Contains(location, StringComparison.OrdinalIgnoreCase)) ||
                                  (user.AdditionalInfo?.Contains(location, StringComparison.OrdinalIgnoreCase) ?? false);

            var serviceMatched = string.IsNullOrWhiteSpace(serviceCategory) ||
                                 serviceCategories.Any(item => item.Equals(serviceCategory, StringComparison.OrdinalIgnoreCase));

            return keywordMatched && locationMatched && serviceMatched;
        })
        .Select(MapToPublicWorkerCard)
        .OrderByDescending(worker => worker.Verified)
        .ThenByDescending(worker => worker.ExperienceYears)
        .ThenByDescending(worker => worker.UpdatedAt)
        .ToList();

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(filtered),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        return filtered;
    }

    public async Task<WorkerInfoResponse?> GetPublicWorkerProfileAsync(Guid workerId, Guid requesterId)
    {
        if (requesterId != Guid.Empty)
        {
            return await GetWorkerInfoAsync(workerId, requesterId);
        }

        var version = await GetWorkerCacheVersionAsync();
        var cacheKey = $"workers:public:detail:v{version}:{workerId}";
        var cached = await _distributedCache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return JsonSerializer.Deserialize<WorkerInfoResponse>(cached);
        }

        var worker = await GetWorkerInfoAsync(workerId, Guid.Empty);
        if (worker == null)
        {
            return null;
        }

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(worker),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        return worker;
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.Latitude = request.Latitude;
        user.Longitude = request.Longitude;
        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        if (request.AdditionalInfo != null)
        {
            user.AdditionalInfo = request.AdditionalInfo;
        }

        if (user.Role == Domain.Enums.Role.Worker)
        {
            user.WorkerProfile ??= new WorkerProfile { UserId = userId };
            user.WorkerProfile.Bio = request.Bio;
            user.WorkerProfile.DesiredJobTitle = request.DesiredJobTitle;
            user.WorkerProfile.SeekingDescription = request.SeekingDescription;
            user.WorkerProfile.ExperienceYears = request.ExperienceYears;
            user.WorkerProfile.HourlyRate = request.HourlyRate;
            user.WorkerProfile.IsProfilePublic = request.IsProfilePublic;
            user.WorkerProfile.Skills = JsonSerializer.Serialize(request.Skills ?? new List<string>());
            user.WorkerProfile.PreferredLocations = JsonSerializer.Serialize(request.PreferredLocations ?? new List<string>());
            user.WorkerProfile.DesiredServiceCategories = JsonSerializer.Serialize(request.DesiredServiceCategories ?? new List<string>());
            user.WorkerProfile.UpdatedAt = DateTime.UtcNow;

            await TouchWorkerCacheVersionAsync();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task MarkPhoneVerifiedAsync(Guid userId, string channel)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        var info = ParseAdditionalInfo(user.AdditionalInfo);
        info["phoneVerified"] = true;
        info["phoneVerifiedAt"] = DateTime.UtcNow;
        info["phoneVerificationChannel"] = channel;
        user.AdditionalInfo = info.ToJsonString();
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task LockUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.IsLocked = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task UnlockUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.IsLocked = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task<AdminUserDetailResponse?> GetUserDetailAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;
        return MapToAdminDetail(user);
    }

    public async Task<AdminUserDetailResponse?> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Email != null) user.Email = request.Email;
        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (request.HasPremiumAccess.HasValue) user.HasPremiumAccess = request.HasPremiumAccess.Value;
        if (request.PremiumExpiry.HasValue) user.PremiumExpiry = request.PremiumExpiry.Value;
        if (request.IsLocked.HasValue) user.IsLocked = request.IsLocked.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();
        return MapToAdminDetail(user);
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        await _userRepository.DeleteAsync(user);
        await _userRepository.SaveChangesAsync();
        await TouchWorkerCacheVersionAsync();
        return true;
    }

    private AdminUserDetailResponse MapToAdminDetail(User user)
    {
        var info = ParseAdditionalInfo(user.AdditionalInfo);
        var phoneVerified = info["phoneVerified"]?.GetValue<bool>() ?? false;
        var phoneVerifiedAt = info["phoneVerifiedAt"]?.GetValue<DateTime?>();
        var phoneVerificationChannel = info["phoneVerificationChannel"]?.GetValue<string>();

        return new AdminUserDetailResponse
        {
            Id = user.Id,
            Phone = user.Phone,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsGuest = user.IsGuest,
            IsLocked = user.IsLocked,
            HasPremiumAccess = user.HasPremiumAccess,
            PremiumExpiry = user.PremiumExpiry,
            AvatarUrl = user.AvatarUrl,
            PhoneVerified = phoneVerified,
            PhoneVerifiedAt = phoneVerifiedAt,
            PhoneVerificationChannel = phoneVerificationChannel,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            AdditionalInfo = user.AdditionalInfo,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            WorkerProfile = user.WorkerProfile == null ? null : new WorkerProfileSummary
            {
                Bio = user.WorkerProfile.Bio,
                DesiredJobTitle = user.WorkerProfile.DesiredJobTitle,
                SeekingDescription = user.WorkerProfile.SeekingDescription,
                ExperienceYears = user.WorkerProfile.ExperienceYears,
                HourlyRate = user.WorkerProfile.HourlyRate,
                Verified = user.WorkerProfile.Verified,
                IsProfilePublic = user.WorkerProfile.IsProfilePublic,
                Skills = user.WorkerProfile.Skills,
                PreferredLocations = user.WorkerProfile.PreferredLocations,
                DesiredServiceCategories = user.WorkerProfile.DesiredServiceCategories
            }
        };
    }

    private WorkerInfoResponse MapToWorkerInfoResponse(User user, bool hasFullAccess)
    {
        var profile = user.WorkerProfile;

        return new WorkerInfoResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = hasFullAccess ? user.Phone : MaskContact(user.Phone),
            Email = hasFullAccess ? user.Email : MaskContact(user.Email),
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender.ToString(),
            DateOfBirth = user.DateOfBirth,
            Age = CalculateAge(user.DateOfBirth),
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            Bio = profile?.Bio,
            DesiredJobTitle = profile?.DesiredJobTitle,
            SeekingDescription = profile?.SeekingDescription,
            ExperienceYears = profile?.ExperienceYears ?? 0,
            HourlyRate = profile?.HourlyRate ?? 0,
            Verified = profile?.Verified ?? false,
            IsProfilePublic = profile?.IsProfilePublic ?? false,
            Skills = DeserializeList(profile?.Skills),
            PreferredLocations = DeserializeList(profile?.PreferredLocations),
            DesiredServiceCategories = DeserializeList(profile?.DesiredServiceCategories),
            JoinedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private PublicWorkerCardResponse MapToPublicWorkerCard(User user)
    {
        var profile = user.WorkerProfile!;

        return new PublicWorkerCardResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            DesiredJobTitle = profile.DesiredJobTitle,
            SeekingDescription = profile.SeekingDescription,
            ExperienceYears = profile.ExperienceYears,
            HourlyRate = profile.HourlyRate,
            Verified = profile.Verified,
            LocationSummary = BuildLocationSummary(user.AdditionalInfo, profile.PreferredLocations),
            Skills = DeserializeList(profile.Skills),
            DesiredServiceCategories = DeserializeList(profile.DesiredServiceCategories),
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static int CalculateAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
        {
            return 0;
        }

        var age = DateTime.Today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static JsonObject ParseAdditionalInfo(string? additionalInfo)
    {
        if (string.IsNullOrWhiteSpace(additionalInfo))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(additionalInfo)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string? BuildLocationSummary(string? additionalInfo, string? preferredLocationsJson)
    {
        var preferredLocations = DeserializeList(preferredLocationsJson);
        if (preferredLocations.Count > 0)
        {
            return string.Join(", ", preferredLocations.Take(3));
        }

        var info = ParseAdditionalInfo(additionalInfo);
        var wardName = info["wardName"]?.GetValue<string>();
        var districtName = info["districtName"]?.GetValue<string>();
        var provinceName = info["provinceName"]?.GetValue<string>();

        var parts = new[] { wardName, districtName, provinceName }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private async Task<string> GetWorkerCacheVersionAsync()
    {
        var version = await _distributedCache.GetStringAsync(PublicWorkerVersionCacheKey);
        if (string.IsNullOrWhiteSpace(version))
        {
            version = DateTime.UtcNow.Ticks.ToString();
            await _distributedCache.SetStringAsync(PublicWorkerVersionCacheKey, version);
        }

        return version;
    }

    private async Task TouchWorkerCacheVersionAsync()
    {
        await _distributedCache.SetStringAsync(PublicWorkerVersionCacheKey, DateTime.UtcNow.Ticks.ToString());
    }

    private static string NormalizeKeyPart(string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? "all"
            : input.Trim().ToLowerInvariant().Replace(" ", "-");
    }

    private static string? MaskContact(string? contact)
    {
        if (string.IsNullOrEmpty(contact)) return contact;
        if (contact.Contains("@"))
        {
            var parts = contact.Split('@');
            return parts[0].Length > 2 ? parts[0][..2] + "***@" + parts[1] : "***@" + parts[1];
        }

        return contact.Length > 4 ? contact[..3] + "****" + contact[^3..] : "****";
    }
}
