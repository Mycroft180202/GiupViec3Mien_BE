using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System.Threading.Tasks;
using System;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class MatchingConsumer : IConsumer<JobPostedEvent>
{
    private readonly IMatchingService _matchingService;
    private readonly IPublishEndpoint _publishEndpoint;

    public MatchingConsumer(IMatchingService matchingService, IPublishEndpoint publishEndpoint)
    {
        _matchingService = matchingService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<JobPostedEvent> context)
    {
        var job = context.Message;
        
        // In a real app, logic to find workers matching skills and location
        // For now, we simulate finding matches and queuing an email notification
        
        // This is where Point 2 logic lives: background calculation
        Console.WriteLine($"[MatchingService] Calculating best matches for Job: {job.Title}");
        
        // Mocking: Notify some users
        // await _publishEndpoint.Publish(new SendEmailMessage("worker@example.com", "New Job Match", $"A new job matches your skills: {job.Title}"));
    }
}
