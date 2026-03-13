using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GiupViec3Mien.Web;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SeedController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("test-matching-data")]
    public async Task<IActionResult> SeedMatchingData()
    {
        // 1. Clear existing test data (Optional, but safer for demo)
        // _context.Reviews.RemoveRange(_context.Reviews);
        // _context.WorkerProfiles.RemoveRange(_context.WorkerProfiles);
        // _context.Jobs.RemoveRange(_context.Jobs);
        // await _context.SaveChangesAsync();

        // 2. Create an Employer
        var employer = new User
        {
            Phone = "0901112223",
            FullName = "Chủ Nhà Quận 1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = Role.Employer
        };
        await _context.Users.AddAsync(employer);

        // 3. Create a Job in District 1, HCM
        var job = new Job
        {
            EmployerId = employer.Id,
            Title = "Dọn dẹp nhà và Nấu ăn trưa",
            Description = "Dọn căn hộ 2 phòng ngủ và chuẩn bị bữa trưa đơn giản cho gia đình 3 người.",
            Location = "Bitexco Building, Quận 1",
            Latitude = 10.7769,
            Longitude = 106.7009,
            Price = 200000,
            Status = JobStatus.Open
        };
        await _context.Jobs.AddAsync(job);

        // 4. Create Worker A: "High Match" (Close, Skilled, Good Rating)
        var workerA = new User
        {
            Phone = "0811222333",
            FullName = "Nguyễn Thị Sạch (Best Match)",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = Role.Worker,
            Latitude = 10.7800,
            Longitude = 106.6950, // ~0.7km away
            WorkerProfile = new WorkerProfile
            {
                ExperienceYears = 6,
                HourlyRate = 180000,
                Verified = true,
                Skills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp", "Nấu ăn", "Giặt là" })
            }
        };

        // 5. Create Worker B: "Skill Match but Far" (Go Vap District)
        var workerB = new User
        {
            Phone = "0822333444",
            FullName = "Trần Văn Bếp (Far Match)",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = Role.Worker,
            Latitude = 10.8231,
            Longitude = 106.6297, // ~10km away
            WorkerProfile = new WorkerProfile
            {
                ExperienceYears = 10,
                HourlyRate = 250000,
                Verified = true,
                Skills = JsonSerializer.Serialize(new List<string> { "Nấu ăn", "Dọn dẹp" })
            }
        };

        // 6. Create Worker C: "Close but Low Rating/Exp"
        var workerC = new User
        {
            Phone = "0833444555",
            FullName = "Lê Văn Mới (Weak Match)",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = Role.Worker,
            Latitude = 10.7750,
            Longitude = 106.7050, // ~0.5km away
            WorkerProfile = new WorkerProfile
            {
                ExperienceYears = 1,
                HourlyRate = 150000,
                Verified = false,
                Skills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp" })
            }
        };

        await _context.Users.AddRangeAsync(workerA, workerB, workerC);
        await _context.SaveChangesAsync();

        // 7. Add Reviews for scores
        var reviews = new List<Review>
        {
            new Review { JobId = job.Id, ReviewerId = employer.Id, RevieweeId = workerA.Id, Rating = 5, Comment = "Rất tuyệt vời" },
            new Review { JobId = job.Id, ReviewerId = employer.Id, RevieweeId = workerA.Id, Rating = 4, Comment = "Làm việc chăm chỉ" },
            new Review { JobId = job.Id, ReviewerId = employer.Id, RevieweeId = workerB.Id, Rating = 5, Comment = "Nấu ăn ngon" },
            new Review { JobId = job.Id, ReviewerId = employer.Id, RevieweeId = workerC.Id, Rating = 2, Comment = "Làm còn chậm" },
            new Review { JobId = job.Id, ReviewerId = employer.Id, RevieweeId = workerC.Id, Rating = 3, Comment = "Chưa có kinh nghiệm" }
        };
        await _context.Reviews.AddRangeAsync(reviews);
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Test matching data seeded successfully", 
            jobId = job.Id,
            employerId = employer.Id,
            workerAId = workerA.Id,
            workers = new[] { "Nguyễn Thị Sạch", "Trần Văn Bếp", "Lê Văn Mới" }
        });
    }
}
