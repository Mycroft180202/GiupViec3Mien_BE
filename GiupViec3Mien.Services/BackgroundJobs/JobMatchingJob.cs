using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using GiupViec3Mien.Services.Interfaces;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("high-priority")]
public class JobMatchingJob
{
    private readonly IMatchingService _matchingService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobMatchingJob(IMatchingService matchingService, IBackgroundJobClient backgroundJobClient)
    {
        _matchingService = matchingService;
        _backgroundJobClient = backgroundJobClient;
    }

    [DisplayName("Calculate Best Worker Matches for Job: {1}")]
    public async Task ExecuteAsync(Guid jobId, string title)
    {
        // 1. Get Best Matches (could be 100+ workers)
        var matches = await _matchingService.GetBestMatchesForJobAsync(jobId, limit: 10);

        foreach (var match in matches)
        {
            if (match.MatchScore >= 70) // High quality match
            {
                // 2. Schedule a reliable email for EACH matching worker
                // Using SendEmailJob ensures each individual email has its own retry policy
                _backgroundJobClient.Enqueue<SendEmailJob>(
                    job => job.SendAsync(
                        "worker-email-placeholder", // Should fetch user email here
                        "New Job Matching Your Skills!", 
                        $"Hi {match.FullName}, the job '{title}' is a {match.MatchScore}% match for your skills. Apply now!")
                );
            }
        }
    }
}
