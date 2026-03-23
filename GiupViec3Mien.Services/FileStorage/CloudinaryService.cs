using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.FileStorage;

public class CloudinaryService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;

    public CloudinaryService(IConfiguration config)
    {
        var cloudinarySettings = config.GetSection("CloudinarySettings");
        _cloudName = cloudinarySettings["CloudName"] ?? throw new InvalidOperationException("Cloudinary CloudName is not configured.");
        
        var account = new Account(
            _cloudName,
            cloudinarySettings["ApiKey"],
            cloudinarySettings["ApiSecret"]
        );

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folderName = "GiupViec3Mien")
    {
        if (file.Length > 0)
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        return string.Empty;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName = "GiupViec3Mien/CVs")
    {
        if (file.Length > 0)
        {
            using var stream = file.OpenReadStream();
            return await UploadCvStreamAsync(stream, file.FileName, folderName);
        }

        return string.Empty;
    }

    public async Task<string> UploadFileAsync(byte[] fileContent, string fileName, string folderName = "GiupViec3Mien/CVs")
    {
        if (fileContent != null && fileContent.Length > 0)
        {
            using var stream = new MemoryStream(fileContent);
            return await UploadCvStreamAsync(stream, fileName, folderName);
        }

        return string.Empty;
    }

    public async Task<string> GetAccessibleFileUrlAsync(string fileUrlOrPublicId)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrPublicId))
        {
            throw new ArgumentException("File URL is empty.", nameof(fileUrlOrPublicId));
        }

        if (Uri.TryCreate(fileUrlOrPublicId, UriKind.Absolute, out var absoluteUri))
        {
            if (IsImageDeliveryUrl(absoluteUri))
            {
                return absoluteUri.ToString();
            }

            var publicIdFromUrl = ExtractPublicId(fileUrlOrPublicId);
            await EnsureRawFileIsPublicAsync(publicIdFromUrl);
            return absoluteUri.ToString();
        }

        var publicId = ExtractPublicId(fileUrlOrPublicId);
        if (IsPdf(publicId))
        {
            return BuildImageFileUrl(publicId);
        }

        await EnsureRawFileIsPublicAsync(publicId);
        return BuildRawFileUrl(publicId);
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        
        return result.Result == "ok";
    }

    private async Task<string> UploadCvStreamAsync(Stream stream, string fileName, string folderName)
    {
        if (IsPdf(fileName))
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folderName,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return FinalizeImageUpload(uploadResult);
        }

        var rawUploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folderName,
            Type = "upload",
            UseFilename = true,
            UniqueFilename = true
        };

        var rawUploadResult = await _cloudinary.UploadAsync(rawUploadParams);
        return await FinalizeRawUploadAsync(rawUploadResult);
    }

    private async Task<string> FinalizeRawUploadAsync(RawUploadResult uploadResult)
    {
        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
        }

        if (string.IsNullOrWhiteSpace(uploadResult.PublicId))
        {
            throw new Exception("Cloudinary did not return a public id for the uploaded file.");
        }

        await EnsureRawFileIsPublicAsync(uploadResult.PublicId);

        return !string.IsNullOrWhiteSpace(uploadResult.SecureUrl?.ToString())
            ? uploadResult.SecureUrl.ToString()
            : BuildRawFileUrl(uploadResult.PublicId);
    }

    private string FinalizeImageUpload(ImageUploadResult uploadResult)
    {
        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
        }

        if (!string.IsNullOrWhiteSpace(uploadResult.SecureUrl?.ToString()))
        {
            return uploadResult.SecureUrl.ToString();
        }

        if (string.IsNullOrWhiteSpace(uploadResult.PublicId))
        {
            throw new Exception("Cloudinary did not return a public id for the uploaded PDF.");
        }

        return BuildImageFileUrl(uploadResult.PublicId);
    }

    private async Task EnsureRawFileIsPublicAsync(string publicId)
    {
        var updateResult = await _cloudinary.UpdateResourceAccessModeByIdsAsync(new UpdateResourceAccessModeParams
        {
            PublicIds = new List<string> { publicId },
            AccessMode = "public",
            ResourceType = ResourceType.Raw,
            Type = "upload"
        });

        if (updateResult?.Error != null)
        {
            var message = updateResult.Error.Message ?? string.Empty;
            if (message.Contains("feature is not enabled", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new Exception($"Cloudinary Access Mode Error: {message}");
        }
    }

    private string ExtractPublicId(string fileUrlOrPublicId)
    {
        if (!Uri.TryCreate(fileUrlOrPublicId, UriKind.Absolute, out var uri))
        {
            return fileUrlOrPublicId.Trim();
        }

        var uploadMarker = "/upload/";
        var absolutePath = uri.AbsolutePath;
        var uploadIndex = absolutePath.IndexOf(uploadMarker, StringComparison.OrdinalIgnoreCase);
        if (uploadIndex < 0)
        {
            throw new Exception("Unsupported Cloudinary file URL.");
        }

        var publicId = absolutePath[(uploadIndex + uploadMarker.Length)..];
        if (publicId.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            var slashIndex = publicId.IndexOf('/');
            if (slashIndex > 1 && publicId[1..slashIndex].All(char.IsDigit))
            {
                publicId = publicId[(slashIndex + 1)..];
            }
        }

        return Uri.UnescapeDataString(publicId);
    }

    private string BuildRawFileUrl(string publicId)
    {
        var escapedPublicId = string.Join("/", publicId.Split('/').Select(Uri.EscapeDataString));
        return $"https://res.cloudinary.com/{_cloudName}/raw/upload/{escapedPublicId}";
    }

    private string BuildImageFileUrl(string publicId)
    {
        var escapedPublicId = string.Join("/", publicId.Split('/').Select(Uri.EscapeDataString));
        return $"https://res.cloudinary.com/{_cloudName}/image/upload/{escapedPublicId}";
    }

    private static bool IsPdf(string fileNameOrPublicId)
    {
        return string.Equals(Path.GetExtension(fileNameOrPublicId), ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageDeliveryUrl(Uri uri)
    {
        return uri.AbsolutePath.Contains("/image/upload/", StringComparison.OrdinalIgnoreCase);
    }
}
