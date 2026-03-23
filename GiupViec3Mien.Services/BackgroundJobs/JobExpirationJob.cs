using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Domain.Enums;
using System;
using System.Threading.Tasks;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("default")]
public class JobExpirationJob : IBackgroundJob
{
    private readonly IJobRepository _jobRepository;

    public JobExpirationJob(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public string JobId => "daily-job-expiration";
    public string CronExpression => "0 0 * * *"; // Run daily at midnight

    public async Task ExecuteAsync()
    {
        Console.WriteLine($"[Hangfire] Starting {JobId} at {DateTime.UtcNow}");
        
        var activeJobs = await _jobRepository.GetActiveJobsAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        foreach (var job in activeJobs)
        {
            if (job.CreatedAt < cutoffDate)
            {
                Console.WriteLine($"[Hangfire] Expiring job: {job.Id} ({job.Title})");
                job.Status = JobStatus.Cancelled;
                job.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _jobRepository.SaveChangesAsync();
        Console.WriteLine($"[Hangfire] Completed {JobId}");
    }
}
