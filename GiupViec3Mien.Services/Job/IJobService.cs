using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Job;

public interface IJobService
{
    Task<JobResponse> CreateJobAsync(Guid employerId, CreateJobRequest request);
    Task<JobResponse?> UpdateJobAsync(Guid userId, Guid jobId, CreateJobRequest request);
    Task<bool> DeleteJobAsync(Guid userId, Guid jobId);

    Task<IEnumerable<JobResponse>> GetAvailableJobsAsync();
    Task<IEnumerable<JobResponse>> GetWorkerAdsAsync();
    
    Task<JobResponse?> GetJobDetailsAsync(Guid jobId, Guid requesterId);
    Task<IEnumerable<JobResponse>> GetJobsByEmployerAsync(Guid employerId);
    Task<IEnumerable<JobResponse>> GetJobsByWorkerAsync(Guid workerId);
    Task<IEnumerable<JobResponse>> GetMyAdsAsync(Guid userId);

    Task<JobApplicationResponse> ApplyToJobAsync(Guid userId, Guid jobId, ApplyJobRequest request);
    Task<IEnumerable<JobApplicationResponse>> GetJobApplicationsAsync(Guid jobId, Guid requesterId);
    Task<IEnumerable<JobApplicationResponse>> GetMyApplicationsAsync(Guid userId);
    Task<IEnumerable<JobApplicationResponse>> GetReceivedApplicationsAsync(Guid employerId);
    Task<JobApplicationResponse?> GetMyApplicationForJobAsync(Guid userId, Guid jobId);
    Task<JobApplicationResponse?> AcceptApplicationAsync(Guid employerId, Guid applicationId);
    Task<IEnumerable<JobResponse>> GetJobsBySkillsAsync(IEnumerable<string> skills);
    Task<IEnumerable<JobResponse>> SearchJobsAsync(JobSearchFilters filters);

    // Admin operations
    Task<IEnumerable<JobResponse>> GetAllJobsAsync();
    Task<bool> DeleteViolatingJobAsync(Guid jobId);
    Task<int> GetTotalApplicationCountAsync();
    Task<JobResponse?> UpdateJobStatusAsync(Guid jobId, JobStatus newStatus);
    Task<IDictionary<JobStatus, int>> GetJobStatusCountsAsync();
}
