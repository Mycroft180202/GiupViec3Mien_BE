using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.Job;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Job;

namespace TestNamespace;

public class SeedEmployerApply
{
    public static async Task Run(IJobService jobService, IUserRepository userRepo, IJobRepository jobRepo)
    {
        Console.WriteLine("--- Seeding Employer Data for Application Workflow ---");

        // 1. Create the Employer (The person who will apply)
        var employerId = Guid.NewGuid();
        var employer = new User
        {
            Id = employerId,
            Phone = "0909123456",
            FullName = "Công ty Dịch vụ Hoàn Mỹ",
            Role = Role.Employer,
            AvatarUrl = "https://example.com/logo.png",
            Latitude = 10.7626,
            Longitude = 106.6602
        };
        await userRepo.AddAsync(employer);

        // 2. Create the Job (The target for application)
        // Note: Usually, another user owns this job. Let's create a Worker who posted a "service demand".
        var workerId = Guid.NewGuid();
        var worker = new User
        {
            Id = workerId,
            FullName = "Nguyễn Thị Giúp Việc",
            Role = Role.Worker,
            Latitude = 10.7620,
            Longitude = 106.6600
        };
        await userRepo.AddAsync(worker);

        var jobId = Guid.NewGuid();
        var job = new Job
        {
            Id = jobId,
            EmployerId = employerId, // In this case, maybe the employer is the one who created it but it's for a worker?
            // Wait, normally EmployerId is the OWNER.
            // If the employer is APPLYING, they shouldn't be the owner.
            Title = "Cần người dọn dẹp căn hộ Quận 10",
            Description = "Cần gấp người dọn dẹp 2 phòng ngủ vào sáng mai.",
            Location = "Quận 10, TP.HCM",
            Price = 200000,
            Status = JobStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        // Let's make the owner a different user for a realistic test
        var ownerId = Guid.NewGuid();
        var owner = new User { Id = ownerId, FullName = "Chủ Nhà A", Role = Role.Employer };
        await userRepo.AddAsync(owner);
        job.EmployerId = ownerId;
        
        await jobRepo.AddAsync(job);
        await userRepo.SaveChangesAsync();
        await jobRepo.SaveChangesAsync();

        Console.WriteLine($"Seeded Employer: {employer.FullName} ({employer.Id})");
        Console.WriteLine($"Seeded Job: {job.Title} ({job.Id}) owned by {owner.FullName}");

        // 3. Simulate Application with Parameters
        var applyRequest = new ApplyJobRequest
        {
            Message = "Công ty chúng tôi có nhân viên sẵn sàng dọn dẹp ngay ngày mai.",
            BidPrice = 180000 // Custom bid lower than job price
        };

        Console.WriteLine("\n--- Testing Application API Parameters ---");
        Console.WriteLine($"Request JSON: {JsonSerializer.Serialize(applyRequest)}");

        try
        {
            var application = await jobService.ApplyToJobAsync(employerId, jobId, applyRequest);
            
            Console.WriteLine("\n--- Resulting Application JSON ---");
            var options = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(application, options));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during application: {ex.Message}");
        }
    }
}
