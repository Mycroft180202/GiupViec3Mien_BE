using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GiupViec3Mien.Persistence.Seeders;

public class SeedJobs
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Get/Create an Employer
        var employer = await context.Users.FirstOrDefaultAsync(u => u.Phone == "0900000123");
        if (employer == null)
        {
            employer = new User
            {
                FullName = "Nguyễn Chủ Nhà",
                Phone = "0900000123",
                Email = "thanhhovn2016@yandex.com",
                PasswordHash = "hashed_pw",
                Role = Domain.Enums.Role.Employer,
                Latitude = 10.7769,
                Longitude = 106.7009
            };
            await context.Users.AddAsync(employer);
            await context.SaveChangesAsync();
        }

        // 2. Clear existing seed jobs if needed (optional)
        // For testing, we keep the ones we added in previous manual steps unless we want a fresh start.
        
        // 3. Define Seed Jobs
        var jobsToSeed = new List<Job>
        {
            new Job
            {
                EmployerId = employer.Id,
                Title = "Dọn dẹp nhà cửa 4 tầng",
                Description = "Cần người dọn dẹp nhà cửa, lau dọn 4 tầng lầu, sạch sẽ, trung thực.",
                Location = "Quận 1, TP.HCM",
                Price = 300000,
                Latitude = 10.7769,
                Longitude = 106.7009,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "Cleaning", "Housework" }),
                Status = Domain.Enums.JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new Job
            {
                EmployerId = employer.Id,
                Title = "Nấu ăn gia đình 4 người",
                Description = "Cần người nấu ăn tối cho gia đình 4 người, thực đơn đa dạng, sạch sẽ.",
                Location = "Quận 3, TP.HCM",
                Price = 200000,
                Latitude = 10.7825,
                Longitude = 106.6836,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "Cooking", "Housework" }),
                Status = Domain.Enums.JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new Job
            {
                EmployerId = employer.Id,
                Title = "Chăm sóc bé 2 tuổi",
                Description = "Cần người chăm sóc bé 2 tuổi ban ngày, yêu trẻ, có kinh nghiệm.",
                Location = "Quận Bình Thạnh, TP.HCM",
                Price = 500000,
                Latitude = 10.8105,
                Longitude = 106.7091,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "BabyCare", "Nanny" }),
                Status = Domain.Enums.JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new Job
            {
                EmployerId = employer.Id,
                Title = "Chăm sóc người già tại nhà",
                Description = "Cần người chăm sóc cụ ông 80 tuổi, đi lại khó khăn, trực đêm.",
                Location = "Quận 7, TP.HCM",
                Price = 600000,
                Latitude = 10.7291,
                Longitude = 106.7217,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "ElderlyCare", "Nursing" }),
                Status = Domain.Enums.JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Job
            {
                EmployerId = employer.Id,
                Title = "Sửa máy lạnh chảy nước",
                Description = "Máy lạnh nhà tôi bị chảy nước, cần thợ qua kiểm tra và vệ sinh.",
                Location = "Quận Tân Bình, TP.HCM",
                Price = 150000,
                Latitude = 10.7937,
                Longitude = 106.6436,
                RequiredSkills = JsonSerializer.Serialize(new List<string> { "Repair", "Electrical" }),
                Status = Domain.Enums.JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        foreach (var job in jobsToSeed)
        {
            if (!await context.Jobs.AnyAsync(j => j.Title == job.Title && j.EmployerId == employer.Id))
            {
                await context.Jobs.AddAsync(job);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("JOB SEED DATA GENERATED:");
        Console.WriteLine($"5 Jobs added/verified for Employer: {employer.FullName}");
        Console.WriteLine("--------------------------------------------------");
    }
}
