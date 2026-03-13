using GiupViec3Mien.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.UserServices;

public interface IUserService
{
    Task<string> UploadProfileImageAsync(Guid userId, IFormFile file);
    Task UpdateSkillsAsync(Guid userId, List<string> skills);
}
