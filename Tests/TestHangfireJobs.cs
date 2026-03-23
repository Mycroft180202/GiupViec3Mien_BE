using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.BackgroundJobs;
using GiupViec3Mien.Services.Interfaces;

namespace TestNamespace;

public class MockEmailService : IEmailService
{
    public List<(string To, string Subject)> SentEmails = new();
    public Task SendEmailAsync(string to, string subject, string body)
    {
        SentEmails.Add((to, subject));
        Console.WriteLine($"[MockEmail] Sent to {to}: {subject}");
        return Task.CompletedTask;
    }
}

public class HangfireJobTests
{
    public static async Task RunTests()
    {
        Console.WriteLine("--- Starting Hangfire Job Logic Tests ---");
        await TestJobExpiration();
        await TestNewsletter();
        await TestProfileReminder();
        Console.WriteLine("--- All Hangfire Job Logic Tests Passed! ---");
    }

    private static async Task TestJobExpiration()
    {
        Console.WriteLine("\n[Test] JobExpirationJob");
        var jobRepo = new MockJobRepository();
        var expiredId = Guid.NewGuid();
        
        // Add an old job (31 days ago)
        jobRepo.Jobs.Add(new Job { 
            Id = expiredId, 
            Title = "Old Job", 
            Status = JobStatus.Open, 
            CreatedAt = DateTime.UtcNow.AddDays(-31) 
        });
        
        // Add a new job (1 day ago)
        jobRepo.Jobs.Add(new Job { 
            Id = Guid.NewGuid(), 
            Title = "New Job", 
            Status = JobStatus.Open, 
            CreatedAt = DateTime.UtcNow.AddDays(-1) 
        });

        var job = new JobExpirationJob(jobRepo);
        await job.ExecuteAsync();

        var expiredJob = jobRepo.Jobs.First(j => j.Id == expiredId);
        if (expiredJob.Status == JobStatus.Cancelled)
            Console.WriteLine("SUCCESS: Old job was expired.");
        else
            throw new Exception("FAIL: Old job was NOT expired.");
    }

    private static async Task TestNewsletter()
    {
        Console.WriteLine("\n[Test] NewsletterJob");
        var userRepo = new MockUserRepository();
        var emailService = new MockEmailService();
        
        userRepo.Users.Add(new User { FullName = "User 1", Email = "user1@test.com" });
        userRepo.Users.Add(new User { FullName = "User 2", Email = "" }); // No email

        var job = new NewsletterJob(userRepo, emailService);
        await job.ExecuteAsync();

        if (emailService.SentEmails.Count == 1 && emailService.SentEmails[0].To == "user1@test.com")
            Console.WriteLine("SUCCESS: Newsletter sent only to users with email.");
        else
            throw new Exception("FAIL: Newsletter logic incorrect.");
    }

    private static async Task TestProfileReminder()
    {
        Console.WriteLine("\n[Test] ProfileReminderJob");
        var userRepo = new MockUserRepository();
        var emailService = new MockEmailService();
        
        var userId = Guid.NewGuid();
        userRepo.Users.Add(new User { 
            Id = userId, 
            FullName = "Lazy Worker", 
            Email = "lazy@worker.com",
            Role = Role.Worker,
            WorkerProfile = new WorkerProfile { Bio = "", Skills = "" } // Incomplete
        });

        var job = new ProfileReminderJob(userRepo, emailService);
        await job.SendReminderAsync(userId);

        if (emailService.SentEmails.Count == 1)
            Console.WriteLine("SUCCESS: Reminder sent for incomplete profile.");
        else
            throw new Exception("FAIL: Reminder NOT sent for incomplete profile.");
    }
}
