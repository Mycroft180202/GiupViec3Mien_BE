using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Notification;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("high-priority")]
public class JobMatchingJob
{
    private readonly IMatchingService _matchingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public JobMatchingJob(IMatchingService matchingService, IBackgroundJobClient backgroundJobClient, IUserRepository userRepository, INotificationService notificationService)
    {
        _matchingService = matchingService;
        _backgroundJobClient = backgroundJobClient;
        _userRepository = userRepository;
        _notificationService = notificationService;
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
                var user = await _userRepository.GetByIdAsync(match.WorkerId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    _backgroundJobClient.Enqueue<SendEmailJob>(
                        job => job.SendAsync(
                            user.Email, 
                            "New Job Matching Your Skills!", 
                            $"Hi {match.FullName}, the job '{title}' is a {match.MatchScore}% match for your skills. Apply now!")
                    );
                }

                await _notificationService.CreateAsync(
                    match.WorkerId,
                    "job_match",
                    "Có công việc phù hợp với bạn",
                    $"Tin '{title}' đang phù hợp khoảng {match.MatchScore}% với hồ sơ của bạn.",
                    $"/viec-lam/{jobId}");
            }
        }
    }
}
