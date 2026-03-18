using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.FileStorage;

public interface IFileStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folderName = "GiupViec3Mien");
    Task<string> UploadFileAsync(IFormFile file, string folderName = "GiupViec3Mien/CVs");
    Task<string> UploadFileAsync(byte[] fileContent, string fileName, string folderName = "GiupViec3Mien/CVs");
    Task<bool> DeleteImageAsync(string publicId);
}
