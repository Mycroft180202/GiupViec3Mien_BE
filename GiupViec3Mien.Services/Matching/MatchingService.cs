using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Entities;
using GiupViec3Mien.Services.DTOs.Matching;
using GiupViec3Mien.Services.Interfaces;

namespace GiupViec3Mien.Services.Matching;

public class MatchingService : IMatchingService
{
    private readonly IJobRepository _jobRepository;
    private readonly IUserRepository _userRepository;
    private readonly IReviewRepository _reviewRepository;

    public MatchingService(
        IJobRepository jobRepository, 
        IUserRepository userRepository, 
        IReviewRepository reviewRepository)
    {
        _jobRepository = jobRepository;
        _userRepository = userRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<List<MatchResultDto>> GetBestMatchesForJobAsync(Guid jobId, int limit = 10)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        // Get all workers from repository
        var workers = await _userRepository.GetAllWorkersAsync();

        var matches = new List<MatchResultDto>();

        foreach (var worker in workers)
        {
            var profile = worker.WorkerProfile!;
            
            // 1. Distance Score (40%)
            double distance = CalculateDistance(job.Latitude, job.Longitude, worker.Latitude, worker.Longitude); 
            
            double distanceScore = 0;
            if (distance <= 2) distanceScore = 40;
            else if (distance <= 5) distanceScore = 30;
            else if (distance <= 10) distanceScore = 15;

            // 2. Skill Match (30%)
            var workerSkills = DeserializeSkills(profile.Skills);
            var jobRequiredSkills = DeserializeSkills(job.RequiredSkills);
            
            List<string> matchedSkills;
            if (jobRequiredSkills.Any())
            {
                matchedSkills = workerSkills.Intersect(jobRequiredSkills, StringComparer.OrdinalIgnoreCase).ToList();
            }
            else
            {
                matchedSkills = workerSkills.Where(s => job.Title.Contains(s, StringComparison.OrdinalIgnoreCase) || job.Description.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            double skillScore = 0;
            if (matchedSkills.Any())
            {
                skillScore = (matchedSkills.Count >= 2) ? 30 : 15;
            }

            // 3. Rating Score (15%)
            var reviews = await _reviewRepository.GetByRevieweeIdAsync(worker.Id);
            double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            double ratingScore = avgRating * 3;

            // 4. Experience & Trust (10%)
            double trustScore = 0;
            if (profile.Verified) trustScore += 5;
            if (profile.ExperienceYears > 5) trustScore += 5;
            else if (profile.ExperienceYears >= 2) trustScore += 3;
            else trustScore += 1;

            // 5. Budget Fit (5%)
            double budgetScore = 0;
            if (job.Price > 0 && profile.HourlyRate > 0)
            {
                decimal ratio = profile.HourlyRate / job.Price;
                if (ratio >= 0.8m && ratio <= 1.2m) budgetScore = 5;
            }

            double totalScore = distanceScore + skillScore + ratingScore + trustScore + budgetScore;

            matches.Add(new MatchResultDto
            {
                WorkerId = worker.Id,
                FullName = worker.FullName,
                AvatarUrl = worker.AvatarUrl,
                MatchScore = Math.Round(totalScore, 2),
                DistanceKm = Math.Round(distance, 2),
                AverageRating = Math.Round(avgRating, 1),
                ReviewCount = reviews.Count(),
                ExperienceYears = profile.ExperienceYears,
                HourlyRate = profile.HourlyRate,
                Verified = profile.Verified,
                MatchedSkills = matchedSkills
            });
        }

        return matches.OrderByDescending(m => m.MatchScore).Take(limit).ToList();
    }

    public async Task<List<MatchResultDto>> GetBestMatchesForEmployerAsync(Guid employerId, int limit = 10)
    {
        var employer = await _userRepository.GetByIdAsync(employerId);
        if (employer == null) throw new Exception("Employer not found.");

        // Try to find the latest open job to use as a template for matching
        var latestJob = await _jobRepository.GetLatestJobByEmployerAsync(employerId);

        // If no job, we match based on employer's location and a generic "Dọn dẹp" (Cleaning) criteria
        double targetLat = latestJob?.Latitude ?? employer.Latitude;
        double targetLong = latestJob?.Longitude ?? employer.Longitude;
        string targetTitle = latestJob?.Title ?? "Dọn dẹp";
        string targetDesc = latestJob?.Description ?? "Cần người giúp việc gia đình";
        decimal targetPrice = latestJob?.Price ?? 0;

        var workers = await _userRepository.GetAllWorkersAsync();
        var matches = new List<MatchResultDto>();

        foreach (var worker in workers)
        {
            var profile = worker.WorkerProfile!;
            
            // 1. Distance Score (40%)
            double distance = CalculateDistance(targetLat, targetLong, worker.Latitude, worker.Longitude); 
            double distanceScore = distance <= 2 ? 40 : (distance <= 5 ? 30 : (distance <= 10 ? 15 : 0));

            // 2. Skill Match (30%)
            var workerSkills = DeserializeSkills(profile.Skills);
            var jobRequiredSkills = DeserializeSkills(latestJob?.RequiredSkills);
            
            List<string> matchedSkills;
            if (jobRequiredSkills.Any())
            {
                matchedSkills = workerSkills.Intersect(jobRequiredSkills, StringComparer.OrdinalIgnoreCase).ToList();
            }
            else
            {
                matchedSkills = workerSkills.Where(s => targetTitle.Contains(s, StringComparison.OrdinalIgnoreCase) || targetDesc.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            double skillScore = matchedSkills.Any() ? ((matchedSkills.Count >= 2) ? 30 : 15) : 0;

            // 3. Rating Score (15%)
            var reviews = await _reviewRepository.GetByRevieweeIdAsync(worker.Id);
            double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            double ratingScore = avgRating * 3;

            // 4. Experience & Trust (10%)
            double trustScore = (profile.Verified ? 5 : 0) + (profile.ExperienceYears > 5 ? 5 : (profile.ExperienceYears >= 2 ? 3 : 1));

            // 5. Budget Fit (5%)
            double budgetScore = 0;
            if (targetPrice > 0 && profile.HourlyRate > 0)
            {
                decimal ratio = profile.HourlyRate / targetPrice;
                if (ratio >= 0.8m && ratio <= 1.2m) budgetScore = 5;
            }
            else if (targetPrice == 0) budgetScore = 5; // Default if no job price is set

            double totalScore = distanceScore + skillScore + ratingScore + trustScore + budgetScore;

            matches.Add(new MatchResultDto
            {
                WorkerId = worker.Id,
                FullName = worker.FullName,
                AvatarUrl = worker.AvatarUrl,
                MatchScore = Math.Round(totalScore, 2),
                DistanceKm = Math.Round(distance, 2),
                AverageRating = Math.Round(avgRating, 1),
                ReviewCount = reviews.Count(),
                ExperienceYears = profile.ExperienceYears,
                HourlyRate = profile.HourlyRate,
                Verified = profile.Verified,
                MatchedSkills = matchedSkills
            });
        }

        return matches.OrderByDescending(m => m.MatchScore).Take(limit).ToList();
    }

    public async Task<double> CalculateDistanceAsync(Guid userId, Guid jobId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        double distance = CalculateDistance(user.Latitude, user.Longitude, job.Latitude, job.Longitude);
        return Math.Round(distance, 2);
    }
    public async Task<double> GetUserRatingAsync(Guid userId)
    {
        var reviews = await _reviewRepository.GetByRevieweeIdAsync(userId);
        if (reviews == null || !reviews.Any()) return 0;
        
        return Math.Round(reviews.Average(r => r.Rating), 1);
    }

    public async Task<List<string>> GetSkillMatchAsync(Guid userId, Guid jobId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        List<string> userSkills;
        if (user.Role == Domain.Enums.Role.Worker && user.WorkerProfile != null)
        {
            userSkills = DeserializeSkills(user.WorkerProfile.Skills);
        }
        else
        {
            var info = string.IsNullOrEmpty(user.AdditionalInfo) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(user.AdditionalInfo) ?? new Dictionary<string, object>();
            
            userSkills = info.ContainsKey("skills") && info["skills"] is JsonElement skillsArr 
                ? skillsArr.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList()
                : new List<string>();
        }

        var jobRequiredSkills = DeserializeSkills(job.RequiredSkills);

        if (jobRequiredSkills.Any())
        {
            return userSkills.Intersect(jobRequiredSkills, StringComparer.OrdinalIgnoreCase).ToList();
        }
        else
        {
            return userSkills.Where(s => job.Title.Contains(s, StringComparison.OrdinalIgnoreCase) || job.Description.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    public async Task<EmployerExperienceDto> GetEmployerExperienceAsync(Guid employerId)
    {
        var user = await _userRepository.GetByIdAsync(employerId);
        if (user == null) throw new Exception("Employer not found.");

        var jobs = await _jobRepository.GetJobsByEmployerAsync(employerId);
        var reviews = await _reviewRepository.GetByRevieweeIdAsync(employerId);

        var totalJobs = jobs.Count();
        var completedJobs = jobs.Count(j => j.Status == Domain.Enums.JobStatus.Completed);
        var avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
        var reviewCount = reviews.Count();

        // Trust Level Logic: Based on completed jobs and rating
        string trustLevel = "Medium";
        if (completedJobs > 10 && avgRating >= 4.5) trustLevel = "High";
        else if (completedJobs < 2 || avgRating < 3.0) trustLevel = "Low";

        return new EmployerExperienceDto
        {
            EmployerId = employerId,
            TotalJobsPosted = totalJobs,
            CompletedJobs = completedJobs,
            AverageRating = avgRating,
            ReviewCount = reviewCount,
            IsVerified = !string.IsNullOrEmpty(user.AvatarUrl), // Basic verification rule for now
            TrustLevel = trustLevel
        };
    }

    public async Task<double> GetBudgetFitScoreAsync(Guid userId, Guid jobId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        if (user.WorkerProfile == null) return 0;

        var workerRate = (decimal)user.WorkerProfile.HourlyRate;
        var jobPrice = job.Price;

        if (workerRate <= 0) return 100; // No requirement from worker

        double fitFactor = (double)(jobPrice / workerRate);
        
        if (fitFactor >= 1.0) return 100; // Fully fits budget
        if (fitFactor >= 0.8) return 80;  // Close match
        if (fitFactor >= 0.5) return 50;  // Partial match
        
        return 0; // Below 50% of expectation is usually a mismatch
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        if (lat2 == 0 && lon2 == 0) return 0;
        
        var d1 = lat1 * (Math.PI / 180.0);
        var num1 = lon1 * (Math.PI / 180.0);
        var d2 = lat2 * (Math.PI / 180.0);
        var num2 = lon2 * (Math.PI / 180.0) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
        return 6371 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
    }

    private List<string> DeserializeSkills(string? skillsJson)
    {
        if (string.IsNullOrEmpty(skillsJson)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(skillsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
