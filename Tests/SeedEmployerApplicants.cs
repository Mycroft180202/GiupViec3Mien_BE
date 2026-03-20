using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GiupViec3Mien.Tests;

public class SeedEmployerApplicants
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Find the default employer
        var employer = await context.Users.FirstOrDefaultAsync(u => u.Phone == "0901234567");
        if (employer == null) return;

        // Find a worker
        var worker = await context.Users.FirstOrDefaultAsync(u => u.Phone == "0987654321");
        if (worker == null) return;

        // Create a test job if not exists
        var job = await context.Jobs.FirstOrDefaultAsync(j => j.EmployerId == employer.Id && j.Title.Contains("Test Recruiter View"));
        if (job == null)
        {
            job = new Job
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Cần người dọn dẹp nhà cửa (Test Recruiter View)",
                Description = "Đây là tin đăng dùng để kiểm tra giao diện Quản lý ứng viên của Chủ thuê.",
                Location = "Hồ Chí Minh",
                Price = 150000,
                Status = Domain.Enums.JobStatus.Open,
                PostType = Domain.Enums.PostType.Hiring,
                ServiceCategory = Domain.Enums.ServiceCategory.Housekeeping,
                CreatedAt = DateTime.UtcNow
            };
            await context.Jobs.AddAsync(job);
        }

        // Create an application
        var exists = await context.JobApplications.AnyAsync(a => a.JobId == job.Id && a.ApplicantId == worker.Id);
        if (!exists)
        {
            var application = new JobApplication
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                ApplicantId = worker.Id,
                Message = "Tôi có 5 năm kinh nghiệm làm việc nhà, rất mong được chị nhận việc ạ. Cháu ở gần đây nên đi lại thuận tiện.",
                BidPrice = 145000,
                AppliedAt = DateTime.UtcNow,
                IsAccepted = false
            };
            await context.JobApplications.AddAsync(application);
        }

        await context.SaveChangesAsync();
    }
}
