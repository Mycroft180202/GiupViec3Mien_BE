using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;

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
}
