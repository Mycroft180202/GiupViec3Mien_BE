using GiupViec3Mien.Services.FileStorage;
using GiupViec3Mien.Services.Interfaces;
using System;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("high-priority")]
public class ProcessCVJob
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IJobApplicationRepository _applicationRepository;

    public ProcessCVJob(IFileStorageService fileStorageService, IJobApplicationRepository applicationRepository)
    {
        _fileStorageService = fileStorageService;
        _applicationRepository = applicationRepository;
    }

    [DisplayName("Process CV for Application: {0}")]
    public async Task ExecuteAsync(Guid userId, Guid jobId, byte[] fileContent, string fileName)
    {
        // 1. Re-read data if necessary or use provided bytes
        // In a real production app, we'd use a Stream or Temp File
        
        // 2. Upload to Cloudinary (Heavily retried if service is down)
        var cvUrl = await _fileStorageService.UploadFileAsync(fileContent, fileName);

        // 3. Update the Application Record
        var application = await _applicationRepository.GetByApplicantAndJobAsync(userId, jobId);
        if (application != null)
        {
            application.CvUrl = cvUrl;
            await _applicationRepository.SaveChangesAsync();
        }
    }
}
