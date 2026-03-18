using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.DTOs.Admin;

namespace GiupViec3Mien.Services.UserServices;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public UserService(IUserRepository userRepository, IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<string> UploadProfileImageAsync(Guid userId, IFormFile file)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        string datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        string folderName = $"{datePath}/{userId}";

        string imageUrl = await _fileStorageService.UploadImageAsync(file, folderName);

        if (!string.IsNullOrEmpty(imageUrl))
        {
            user.AvatarUrl = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();
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
    }

    public async Task UpdateSkillsAsync(Guid userId, List<string> skills)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        if (user.Role == Domain.Enums.Role.Worker)
        {
            if (user.WorkerProfile == null)
            {
                user.WorkerProfile = new WorkerProfile { UserId = userId };
            }
            user.WorkerProfile.Skills = JsonSerializer.Serialize(skills);
            user.WorkerProfile.UpdatedAt = DateTime.UtcNow;
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

    public async Task<DTOs.User.WorkerInfoResponse?> GetWorkerInfoAsync(Guid workerId, Guid requesterId)
    {
        var user = await _userRepository.GetByIdAsync(workerId);
        if (user == null || user.Role != Domain.Enums.Role.Worker) return null;

        var profile = user.WorkerProfile;
        var requester = requesterId != Guid.Empty ? await _userRepository.GetByIdAsync(requesterId) : null;

        bool hasFullAccess = workerId == requesterId || 
                            (requester != null && 
                            (requester.Role == Domain.Enums.Role.Admin || 
                            (requester.HasPremiumAccess && requester.PremiumExpiry >= DateTime.UtcNow)));

        int age = 0;
        if (user.DateOfBirth.HasValue)
        {
            age = DateTime.Today.Year - user.DateOfBirth.Value.Year;
            if (user.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
        }

        return new DTOs.User.WorkerInfoResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = hasFullAccess ? user.Phone : MaskContact(user.Phone),
            Email = hasFullAccess ? user.Email : MaskContact(user.Email),
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender.ToString(),
            DateOfBirth = user.DateOfBirth,
            Age = age,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            JoinedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,

            // Profile info
            Bio = profile?.Bio,
            ExperienceYears = profile?.ExperienceYears ?? 0,
            HourlyRate = profile?.HourlyRate ?? 0,
            Verified = profile?.Verified ?? false,
            Skills = string.IsNullOrEmpty(profile?.Skills) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(profile.Skills) ?? new List<string>()
        };
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
        return true;
    }

    private AdminUserDetailResponse MapToAdminDetail(User user)
    {
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
                ExperienceYears = user.WorkerProfile.ExperienceYears,
                HourlyRate = user.WorkerProfile.HourlyRate,
                Verified = user.WorkerProfile.Verified,
                Skills = user.WorkerProfile.Skills
            }
        };
    }

    private string? MaskContact(string? contact)
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
