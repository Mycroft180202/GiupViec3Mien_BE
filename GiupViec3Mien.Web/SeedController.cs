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

    [HttpPost("employers")]
    public async Task<IActionResult> SeedEmployers()
    {
        // 1. Create a Worker seeking jobs
        var worker = new User
        {
            Phone = "0777555666",
            FullName = "Nguyễn Thị Sạch",
            Role = Role.Worker,
            Latitude = 10.7769,
            Longitude = 106.7009,
            WorkerProfile = new WorkerProfile
            {
                HourlyRate = 50000,
                ExperienceYears = 8,
                Verified = true,
                Skills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp", "Nấu ăn" })
            }
        };
        await _context.Users.AddAsync(worker);

        // 2. Clear then Create 3 Diverse Employers
        var emp1 = new User { Phone = "0911111111", FullName = "Chủ Nhà Tốt Bụng (Excelent)", Role = Role.Employer, AvatarUrl = "v.png" };
        var emp2 = new User { Phone = "0922222222", FullName = "Chủ Nhà Tạm Được (Average)", Role = Role.Employer, AvatarUrl = "b.png" };
        var emp3 = new User { Phone = "0933333333", FullName = "Chủ Nhà Xa (Poor)", Role = Role.Employer };

        await _context.Users.AddRangeAsync(emp1, emp2, emp3);
        await _context.SaveChangesAsync();

        // 3. Create Open Jobs for each
        var job1 = new Job {
            EmployerId = emp1.Id, Title = "Dọn biệt thự Quận 1", Latitude = 10.7770, Longitude = 106.7010,
            Price = 70000, RequiredSkills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp", "Nấu ăn" }),
            Status = JobStatus.Open
        };
        var job2 = new Job {
            EmployerId = emp2.Id, Title = "Giúp việc theo giờ", Latitude = 10.8000, Longitude = 106.7100,
            Price = 50000, RequiredSkills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp" }),
            Status = JobStatus.Open
        };
        var job3 = new Job {
            EmployerId = emp3.Id, Title = "Bốc xếp Bình Dương", Latitude = 11.0000, Longitude = 107.0000,
            Price = 30000, Status = JobStatus.Open
        };

        await _context.Jobs.AddRangeAsync(job1, job2, job3);
        
        // 4. Add History for Emp 1 (High Trust)
        for(int i=0; i<15; i++) await _context.Jobs.AddAsync(new Job { EmployerId = emp1.Id, Status = JobStatus.Completed });
        for(int i=0; i<10; i++) await _context.Reviews.AddAsync(new Review { RevieweeId = emp1.Id, Rating = 5, JobId = job1.Id, ReviewerId = worker.Id });
        
        await _context.SaveChangesAsync();

        return Ok(new { 
            workerId = worker.Id, 
            employers = new[] { emp1.FullName, emp2.FullName, emp3.FullName } 
        });
    }
}
