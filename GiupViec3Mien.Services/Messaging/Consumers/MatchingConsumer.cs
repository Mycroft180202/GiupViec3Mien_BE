using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System.Threading.Tasks;
using System;
using Hangfire;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class MatchingConsumer : IConsumer<JobPostedEvent>
{
    private readonly IMatchingService _matchingService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public MatchingConsumer(IMatchingService matchingService, IPublishEndpoint publishEndpoint, IBackgroundJobClient backgroundJobClient)
    {
        _matchingService = matchingService;
        _publishEndpoint = publishEndpoint;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task Consume(ConsumeContext<JobPostedEvent> context)
    {
        var job = context.Message;
        
        // Offload heavy calculation to Hangfire
        _backgroundJobClient.Enqueue<BackgroundJobs.JobMatchingJob>(
            jobManager => jobManager.ExecuteAsync(job.JobId, job.Title));
    }
}
