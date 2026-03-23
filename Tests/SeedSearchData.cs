using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GiupViec3Mien.Persistence;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;

namespace TestNamespace;

public class SeedSearchData
{
    public static async Task Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // Using common dev connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=GiupViec3Mien;Username=postgres;Password=postgres", o => o.UseNetTopologySuite());

        using var context = new ApplicationDbContext(optionsBuilder.Options);

        Console.WriteLine("Cleaning old seed data...");
        // Keep it simple for testing: only clear jobs with certain titles
        var existingJobs = await context.Jobs.Where(j => j.Title.Contains("(Test)")).ToListAsync();
        context.Jobs.RemoveRange(existingJobs);

        var owner = await context.Users.FirstOrDefaultAsync(u => u.Role == Role.Employer);
        if (owner == null)
        {
            owner = new User { Id = Guid.NewGuid(), FullName = "Nguyễn Văn Chủ Nhà (Test)", Role = Role.Employer, Phone = "0123456789" };
            context.Users.Add(owner);
        }

        var newJobs = new List<Job>
        {
            new Job { Id = Guid.NewGuid(), EmployerId = owner.Id, Title = "Giúp việc gia đình Cầu Giấy (Test)", Location = "Cầu Giấy, Hà Nội", Price = 100000, ServiceCategory = ServiceCategory.Housekeeping, PostType = PostType.Hiring, Status = JobStatus.Open, CreatedAt = DateTime.UtcNow },
            new Job { Id = Guid.NewGuid(), EmployerId = owner.Id, Title = "Trông trẻ sơ sinh Quận 1 (Test)", Location = "Quận 1, Hồ Chí Minh", Price = 150000, ServiceCategory = ServiceCategory.Babysitting, PostType = PostType.Hiring, Status = JobStatus.Open, CreatedAt = DateTime.UtcNow },
            new Job { Id = Guid.NewGuid(), EmployerId = owner.Id, Title = "Nấu ăn tối Đống Đa (Test)", Location = "Đống Đa, Hà Nội", Price = 120000, ServiceCategory = ServiceCategory.Cooking, PostType = PostType.Hiring, Status = JobStatus.Open, CreatedAt = DateTime.UtcNow },
            new Job { Id = Guid.NewGuid(), EmployerId = owner.Id, Title = "Hỗ trợ người già Đà Nẵng (Test)", Location = "Hải Châu, Đà Nẵng", Price = 200000, ServiceCategory = ServiceCategory.ElderCare, PostType = PostType.Hiring, Status = JobStatus.Open, CreatedAt = DateTime.UtcNow }

        };

        context.Jobs.AddRange(newJobs);
        await context.SaveChangesAsync();
        Console.WriteLine($"Successfully seeded {newJobs.Count} jobs into the database.");
        Console.WriteLine("Next: Run the /api/Job/reindex-elasticsearch endpoint to sync to Elasticsearch.");
    }
}
