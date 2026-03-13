using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Job;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;

    public JobService(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobResponse> CreateJobAsync(Guid employerId, CreateJobRequest request)
    {
        var job = new Domain.Entities.Job
        {
            EmployerId = employerId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Price = request.Price
        };

        await _jobRepository.AddAsync(job);
        await _jobRepository.SaveChangesAsync();

        return MapToResponse(job);
    }

    public async Task<IEnumerable<JobResponse>> GetAvailableJobsAsync()
    {
        var jobs = await _jobRepository.GetActiveJobsAsync();
        return jobs.Select(MapToResponse);
    }

    public async Task<JobResponse?> GetJobDetailsAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return null;

        return MapToResponse(job);
    }

    private JobResponse MapToResponse(Domain.Entities.Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            EmployerId = job.EmployerId,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            Price = job.Price,
            Status = job.Status,
            CreatedAt = job.CreatedAt
        };
    }
}
