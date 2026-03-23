using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.DTOs.Matching;
using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Matching;

namespace TestNamespace;

// Manual Mocks
public class MockJobRepository : IJobRepository
{
    public List<Job> Jobs = new();
    public Task AddAsync(Job job) { Jobs.Add(job); return Task.CompletedTask; }
    public Task<Job?> GetByIdAsync(Guid id) => Task.FromResult(Jobs.FirstOrDefault(j => j.Id == id));
    public Task<Job?> GetLatestJobByEmployerAsync(Guid employerId) => Task.FromResult(Jobs.Where(j => j.EmployerId == employerId).OrderByDescending(j => j.CreatedAt).FirstOrDefault());
    public Task<IEnumerable<Job>> GetActiveJobsAsync() => Task.FromResult(Jobs.Where(j => j.Status == JobStatus.Open).AsEnumerable());
    public Task<IEnumerable<Job>> GetJobsByEmployerAsync(Guid employerId) => Task.FromResult(Jobs.Where(j => j.EmployerId == employerId).AsEnumerable());
    public Task<IEnumerable<Job>> GetJobsBySkillsAsync(IEnumerable<string> skills) => Task.FromResult(Jobs.Where(j => !string.IsNullOrEmpty(j.RequiredSkills) && skills.Any(s => j.RequiredSkills.Contains(s))).AsEnumerable());
    public Task DeleteAsync(Job job) { Jobs.Remove(job); return Task.CompletedTask; }
    public Task<IEnumerable<Job>> GetJobsByPostTypeAsync(PostType postType) => Task.FromResult(Jobs.Where(j => j.PostType == postType).AsEnumerable());
    public Task<IEnumerable<Job>> GetAllAsync() => Task.FromResult(Jobs.AsEnumerable());
    public Task<IEnumerable<Job>> SearchAsync(string? keyword, ServiceCategory? category, string? location, decimal? minPrice, decimal? maxPrice, JobTimingType? timing, PostType postType) => Task.FromResult(Jobs.AsEnumerable());
    public Task<IEnumerable<Job>> GetCreatedSinceAsync(DateTime date) => Task.FromResult(Jobs.Where(j => j.CreatedAt >= date).AsEnumerable());
    public Task<IEnumerable<Job>> GetByAssignedWorkerIdAsync(Guid workerId) => Task.FromResult(Jobs.Where(j => j.Applications != null && j.Applications.Any(a => a.ApplicantId == workerId)).AsEnumerable());

    public Task SaveChangesAsync() => Task.CompletedTask;
}



public class MockUserRepository : IUserRepository
{
    public List<User> Users = new();
    public Task<User?> GetByPhoneAsync(string phone) => Task.FromResult(Users.FirstOrDefault(u => u.Phone == phone));
    public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    public Task<IEnumerable<User>> GetAllWorkersAsync() => Task.FromResult(Users.Where(u => u.Role == Role.Worker).AsEnumerable());
    public Task<IEnumerable<User>> GetAllAsync() => Task.FromResult(Users.AsEnumerable());
    public Task AddAsync(User user) { Users.Add(user); return Task.CompletedTask; }
    public Task DeleteAsync(User user) { Users.Remove(user); return Task.CompletedTask; }
    public Task SaveChangesAsync() => Task.CompletedTask;
}


public class MockReviewRepository : IReviewRepository
{
    public List<Review> Reviews = new();
    public Task<IEnumerable<Review>> GetByRevieweeIdAsync(Guid revieweeId) => Task.FromResult(Reviews.Where(r => r.RevieweeId == revieweeId).AsEnumerable());
    public Task<Review?> GetReviewAsync(Guid jobId, Guid reviewerId, Guid revieweeId) => 
        Task.FromResult(Reviews.FirstOrDefault(r => r.JobId == jobId && r.ReviewerId == reviewerId && r.RevieweeId == revieweeId));
    public Task AddAsync(Review review) { Reviews.Add(review); return Task.CompletedTask; }
}

public class Program
{
    public static async Task Main()
    {
        var jobRepo = new MockJobRepository();
        var userRepo = new MockUserRepository();
        var reviewRepo = new MockReviewRepository();
        var matchingService = new MatchingService(jobRepo, userRepo, reviewRepo);

        // --- HANGFIRE JOB TESTS ---
        await HangfireJobTests.RunTests();

        // --- ORIGINAL MATCHING TESTS ---
        Console.WriteLine("\n[MatchingTest] Starting User Search Test...");

        // --- THE UNIQUE SEARCH PIVOT (Worker ID) ---
        var searchId = Guid.NewGuid();
        var worker = new User {
            Id = searchId, FullName = "Worker Search Agent", Role = Role.Worker,
            Latitude = 10.7769, Longitude = 106.7009,
            WorkerProfile = new WorkerProfile { 
                HourlyRate = 50000, ExperienceYears = 8, Verified = true,
                Skills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp", "Nấu ăn" }) 
            }
        };
        await userRepo.AddAsync(worker);

        // --- EMPLOYERS ---
        
        // Employer 1: Has TWO different jobs. We expect only ONE entry in results.
        var e1Id = Guid.NewGuid();
        var e1 = new User { Id = e1Id, FullName = "Employer Multi-Job", AvatarUrl = "v.png" };
        await userRepo.AddAsync(e1);
        // Job 1: Good match
        await jobRepo.AddAsync(new Job { Id = Guid.NewGuid(), EmployerId = e1Id, Employer = e1, Title = "Best Job", Latitude = 10.7770, Longitude = 106.7010, Price = 80000, Status = JobStatus.Open, RequiredSkills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp", "Nấu ăn" }) });
        // Job 2: Moderate match
        await jobRepo.AddAsync(new Job { Id = Guid.NewGuid(), EmployerId = e1Id, Employer = e1, Title = "Alt Job", Latitude = 10.7800, Longitude = 106.6900, Price = 50000, Status = JobStatus.Open, RequiredSkills = JsonSerializer.Serialize(new List<string> { "Nấu ăn" }) });

        // Employer 2: Distinct Employer
        var e2Id = Guid.NewGuid();
        var e2 = new User { Id = e2Id, FullName = "Employer Single-Job" };
        await userRepo.AddAsync(e2);
        await jobRepo.AddAsync(new Job { Id = Guid.NewGuid(), EmployerId = e2Id, Employer = e2, Title = "Other Job", Latitude = 10.8000, Longitude = 106.7200, Price = 55000, Status = JobStatus.Open, RequiredSkills = JsonSerializer.Serialize(new List<string> { "Dọn dẹp" }) });

        // --- EXECUTE MATCHING ---
        var results = await matchingService.GetBestJobsForWorkerAsync(searchId, limit: 10);
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        Console.WriteLine(JsonSerializer.Serialize(results, options));
    }
}
