using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System.Threading.Tasks;
using System;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class ApplicationConsumer : IConsumer<JobApplicationTask>
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ApplicationConsumer(IJobApplicationRepository applicationRepository, IJobRepository jobRepository, IPublishEndpoint publishEndpoint)
    {
        _applicationRepository = applicationRepository;
        _jobRepository = jobRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<JobApplicationTask> context)
    {
        var task = context.Message;
        
        // Point 5: Handling Bursts - Logic is pulled from queue and saved sequentially
        var application = new JobApplication
        {
            JobId = task.JobId,
            ApplicantId = task.UserId,
            Message = task.Message,
            BidPrice = task.BidPrice,
            CvUrl = task.CvUrl
        };

        await _applicationRepository.AddAsync(application);
        await _applicationRepository.SaveChangesAsync();

        // After saving, we can queue the email notification (Point 1)
        var job = await _jobRepository.GetByIdAsync(task.JobId);
        if (job?.Employer != null && !string.IsNullOrEmpty(job.Employer.Email))
        {
            Console.WriteLine($"[ApplicationConsumer] Queuing email to employer: {job.Employer.Email}");
            await _publishEndpoint.Publish(new SendEmailMessage(
                job.Employer.Email, 
                $"New application for: {job.Title}",
                $"Someone applied for your job with price {task.BidPrice:N0} VND."
            ));
        }
        else
        {
            Console.WriteLine($"[ApplicationConsumer] Employer email NOT found for job {task.JobId}. Employer null? {job?.Employer == null}");
        }
    }
}
