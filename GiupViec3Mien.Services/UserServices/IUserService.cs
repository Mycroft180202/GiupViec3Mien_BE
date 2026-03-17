using GiupViec3Mien.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.UserServices;

public interface IUserService
{
    Task<string> UploadProfileImageAsync(Guid userId, IFormFile file);
    Task UpdateProfileImageUrlAsync(Guid userId, string imgUrl);
    Task UpdateSkillsAsync(Guid userId, List<string> skills);
    Task UpdateEmailAsync(Guid userId, string email);
}
