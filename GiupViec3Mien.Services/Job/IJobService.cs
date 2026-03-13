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
}
