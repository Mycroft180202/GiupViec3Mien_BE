using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.Interfaces;

namespace TestNamespace;

public class SeedJobDataForApply
{
    public static async Task GenerateJobs(IJobRepository jobRepo, IUserRepository userRepo)
    {
        Console.WriteLine("--- Generating Job Data (Service Demands) for Application Testing ---");

        // 1. Create a Home Owner (The Job Poster)
        var ownerId = Guid.NewGuid();
        var owner = new User
        {
            Id = ownerId,
            Phone = "0912345678",
            FullName = "Bà Nguyễn Thị Chủ Nhà",
            Role = Role.Employer,
            Latitude = 10.7769,
            Longitude = 106.7009
        };
        await userRepo.AddAsync(owner);

        // 2. Generate various "Service Demands" (Jobs)
        var jobsToSeed = new List<Job>
        {
            new Job
            {
                Id = Guid.NewGuid(),
                EmployerId = ownerId,
                Employer = owner,
                Title = "Dọn dẹp nhà cửa định kỳ (Quận 1)",
                Description = "Cần người dọn dẹp nhà 3 tầng, làm việc vào sáng thứ 7 hàng tuần. Yêu cầu trung thực, sạch sẽ.",
                Location = "Quận 1, TP.HCM",
                Price = 150000, // Hourly rate or Fixed price
                Status = JobStatus.Open,
                Latitude = 10.7750,
                Longitude = 106.7010,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp", "Sắp xếp đồ đạc" }),
                CreatedAt = DateTime.UtcNow
            },
            new Job
            {
                Id = Guid.NewGuid(),
                EmployerId = ownerId,
                Employer = owner,
                Title = "Nấu ăn gia đình buổi chiều",
                Description = "Tìm người nấu cơm tối cho gia đình 4 người. Đi chợ và nấu các món cơ bản.",
                Location = "Bình Thạnh, TP.HCM",
                Price = 120000,
                Status = JobStatus.Open,
                Latitude = 10.7950,
                Longitude = 106.7100,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "Nấu ăn", "Đi chợ" }),
                CreatedAt = DateTime.UtcNow
            },
            new Job
            {
                Id = Guid.NewGuid(),
                EmployerId = ownerId,
                Employer = owner,
                Title = "Chăm sóc em bé 1 tuổi",
                Description = "Cần người có kinh nghiệm chăm sóc trẻ em. Làm việc giờ hành chính.",
                Location = "Quận 7, TP.HCM",
                Price = 8000000, // Monthly salary
                Status = JobStatus.Open,
                Latitude = 10.7280,
                Longitude = 106.7050,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "Chăm sóc trẻ", "Sơ cứu cơ bản" }),
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var job in jobsToSeed)
        {
            await jobRepo.AddAsync(job);
            Console.WriteLine($"Generated Job: {job.Title} | ID: {job.Id} | Skills: {job.RequiredSkills}");
        }

        await jobRepo.SaveChangesAsync();
        await userRepo.SaveChangesAsync();

        Console.WriteLine("\n--- Analysis of Job Data ---");
        Console.WriteLine("1. Visibility: Only jobs with Status = 'Open' are visible to applicants.");
        Console.WriteLine("2. Geo-Fencing: Latitude/Longitude are used to calculate DistanceKm for the matching score.");
        Console.WriteLine("3. Skill-Gap: RequiredSkills are compared against the Applicant's skills profile.");
        Console.WriteLine("4. Budget: The Price field serves as the baseline for the application's 'BidPrice'.");
    }
}
