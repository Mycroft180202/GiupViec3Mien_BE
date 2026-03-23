using GiupViec3Mien.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.DTOs.Admin;

namespace GiupViec3Mien.Services.UserServices;

public interface IUserService
{
    Task<string> UploadProfileImageAsync(Guid userId, IFormFile file);
    Task UpdateProfileImageUrlAsync(Guid userId, string imgUrl);
    Task<AdminUserDetailResponse?> GetProfileAsync(Guid userId);
    Task UpdateSkillsAsync(Guid userId, List<string> skills);
    Task UpdateEmailAsync(Guid userId, string email);
    Task<DTOs.User.WorkerInfoResponse?> GetWorkerInfoAsync(Guid workerId, Guid requesterId);
    Task UpdateProfileAsync(Guid userId, DTOs.User.UpdateUserProfileRequest request);
    
    // Admin operations
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<AdminUserDetailResponse?> GetUserDetailAsync(Guid userId);
    Task<AdminUserDetailResponse?> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request);
    Task<bool> DeleteUserAsync(Guid userId);
    Task UpdateWorkerProfileDirectlyAsync(Guid userId, double hourlyRate, string bio, List<string> skills);

    Task LockUserAsync(Guid userId);
    Task UnlockUserAsync(Guid userId);
    Task<IEnumerable<User>> GetAllWorkersWithProfilesAsync();
}

