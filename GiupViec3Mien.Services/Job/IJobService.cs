using GiupViec3Mien.Services.DTOs.Job;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Job;

public interface IJobService
{
    Task<JobResponse> CreateJobAsync(Guid employerId, CreateJobRequest request);
    Task<IEnumerable<JobResponse>> GetAvailableJobsAsync();
    Task<JobResponse?> GetJobDetailsAsync(Guid jobId);
    Task<IEnumerable<JobResponse>> GetJobsByEmployerAsync(Guid employerId);
    Task<JobApplicationResponse> ApplyToJobAsync(Guid userId, Guid jobId, ApplyJobRequest request);
    Task<IEnumerable<JobApplicationResponse>> GetJobApplicationsAsync(Guid jobId);
    Task<IEnumerable<JobApplicationResponse>> GetMyApplicationsAsync(Guid userId);
    Task<JobApplicationResponse?> GetMyApplicationForJobAsync(Guid userId, Guid jobId);
    Task<JobApplicationResponse?> AcceptApplicationAsync(Guid employerId, Guid applicationId);
}
