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

        // Parse target age range (e.g., "20-40")
        int? minAge = null;
        int? maxAge = null;
        if (!string.IsNullOrEmpty(job.TargetAgeRange))
        {
            var parts = job.TargetAgeRange.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
            {
                minAge = min;
                maxAge = max;
            }
            else if (int.TryParse(job.TargetAgeRange, out var age))
            {
                minAge = age - 5;
                maxAge = age + 5;
            }
        }

        foreach (var worker in workers)
        {
            var profile = worker.WorkerProfile!;
            
            // 1. Distance Score (30%)
            double distance = CalculateDistance(job.Latitude, job.Longitude, worker.Latitude, worker.Longitude); 
            double distanceScore = distance <= 2 ? 30 : (distance <= 5 ? 20 : (distance <= 10 ? 10 : 0));

            // 2. Skill Match (25%)
            var workerSkills = DeserializeSkills(profile.Skills);
            var jobRequiredSkills = DeserializeSkills(job.RequiredSkills);
            List<string> matchedSkills = jobRequiredSkills.Any() 
                ? workerSkills.Intersect(jobRequiredSkills, StringComparer.OrdinalIgnoreCase).ToList()
                : workerSkills.Where(s => job.Title.Contains(s, StringComparison.OrdinalIgnoreCase) || job.Description.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            
            double skillScore = matchedSkills.Any() ? (matchedSkills.Count >= 2 ? 25 : 15) : 0;

            // 3. Gender Match (15%)
            double genderScore = 0;
            if (job.PreferredGender == Domain.Enums.GenderOption.Any || job.PreferredGender == worker.Gender)
            {
                genderScore = 15;
            }

            // 4. Age Match (15%)
            double ageScore = 0;
            int? workerAge = null;
            if (worker.DateOfBirth.HasValue)
            {
                workerAge = DateTime.Today.Year - worker.DateOfBirth.Value.Year;
                if (worker.DateOfBirth.Value.Date > DateTime.Today.AddYears(-workerAge.Value)) workerAge--;

                if (minAge.HasValue && maxAge.HasValue)
                {
                    if (workerAge >= minAge && workerAge <= maxAge) ageScore = 15;
                    else if (workerAge >= minAge - 5 && workerAge <= maxAge + 5) ageScore = 7;
                }
                else ageScore = 10; // Default score for having age if no range set
            }
            else if (!minAge.HasValue) ageScore = 10; // Neutral if no age info on either side

            // 5. Experience Score (10%)
            double experienceScore = profile.ExperienceYears >= 5 ? 10 : (profile.ExperienceYears >= 2 ? 7 : (profile.ExperienceYears >= 1 ? 4 : 1));
            if (profile.Verified) experienceScore += 2; // Bonus for verified

            // 6. Rating Score (5%)
            var reviews = await _reviewRepository.GetByRevieweeIdAsync(worker.Id);
            double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            double ratingScore = avgRating; // max 5

            double totalScore = distanceScore + skillScore + genderScore + ageScore + experienceScore + ratingScore;

            matches.Add(new MatchResultDto
            {
                WorkerId = worker.Id,
                FullName = worker.FullName,
                AvatarUrl = worker.AvatarUrl,
                MatchScore = Math.Round(totalScore, 2),
                Gender = worker.Gender.ToString(),
                Age = workerAge,
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

    public async Task<List<EmployerMatchResultDto>> GetBestJobsForWorkerAsync(Guid workerId, int limit = 10)
    {
        var worker = await _userRepository.GetByIdAsync(workerId);
        if (worker == null || worker.Role != Domain.Enums.Role.Worker) 
            throw new Exception("Worker not found.");

        var activeJobs = await _jobRepository.GetActiveJobsAsync();
        var matches = new List<EmployerMatchResultDto>();

        // Find the best job for EACH employer matching this worker
        var employerGroups = activeJobs
            .Where(j => j.EmployerId != Guid.Empty)
            .GroupBy(j => j.EmployerId);

        foreach (var group in employerGroups)
        {
            var employerId = group.Key;
            EmployerMatchResultDto? bestMatchForThisEmployer = null;
            double highestScore = -1;

            foreach (var job in group)
            {
                // 1. Distance Score (30%)
                double distance = CalculateDistance(worker.Latitude, worker.Longitude, job.Latitude, job.Longitude);
                double distanceScore = distance <= 2 ? 30 : (distance <= 5 ? 20 : (distance <= 10 ? 10 : 0));

                // 2. Skill Match (25%)
                var workerSkills = DeserializeSkills(worker.WorkerProfile?.Skills);
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
                double skillScore = matchedSkills.Any() ? (matchedSkills.Count >= 2 ? 25 : 15) : 0;

                // 3. Employer Rating (15%)
                var reviews = await _reviewRepository.GetByRevieweeIdAsync(employerId);
                double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                double ratingScore = avgRating * 3; // 5.0 * 3 = 15

                // 4. Employer Experience & Trust (15%)
                var employerJobs = await _jobRepository.GetJobsByEmployerAsync(employerId);
                int totalJobs = employerJobs.Count();
                int completedJobs = employerJobs.Count(j => j.Status == Domain.Enums.JobStatus.Completed);
                
                double experienceScore = completedJobs > 10 ? 15 : (completedJobs > 5 ? 10 : (completedJobs > 0 ? 5 : 0));

                // Trust level string logic
                string trustLevel = "Medium";
                if (completedJobs >= 10 && avgRating >= 4.5) trustLevel = "High";
                else if (completedJobs < 2 || (reviews.Any() && avgRating < 3.0)) trustLevel = "Low";

                // 5. Budget Fit (10%)
                double budgetFitScore = 0;
                if (worker.WorkerProfile != null && worker.WorkerProfile.HourlyRate > 0)
                {
                    decimal ratio = job.Price / worker.WorkerProfile.HourlyRate;
                    if (ratio >= 1.0m) budgetFitScore = 10;
                    else if (ratio >= 0.8m) budgetFitScore = 8;
                    else if (ratio >= 0.5m) budgetFitScore = 5;
                }
                else budgetFitScore = 10;

                // 6. Feedback Score (Specific history with this employer if any)
                var specificReview = await _reviewRepository.GetReviewAsync(Guid.Empty, worker.Id, employerId);
                double feedbackScore = specificReview?.Rating ?? 0;
                double feedbackBonus = feedbackScore > 0 ? 5 : 0;

                // 7. Verification (Included in visual but score part of trust)
                double verificationScore = !string.IsNullOrEmpty(job.Employer?.AvatarUrl) ? 5 : 0;

                double totalMatchScore = Math.Min(100, distanceScore + skillScore + ratingScore + experienceScore + budgetFitScore + feedbackBonus);

                if (totalMatchScore > highestScore)
                {
                    highestScore = totalMatchScore;
                    bestMatchForThisEmployer = new EmployerMatchResultDto
                    {
                        EmployerId = employerId,
                        JobId = workerId, // CONSISTENT JobId for this search as requested
                        JobTitle = job.Title,
                        EmployerName = job.Employer?.FullName ?? "Unknown",
                        AvatarUrl = job.Employer?.AvatarUrl,
                        MatchScore = Math.Round(totalMatchScore, 2),
                        DistanceKm = Math.Round(distance, 2),
                        AverageRating = Math.Round(avgRating, 1),
                        ReviewCount = reviews.Count(),
                        TotalJobsPosted = totalJobs,
                        CompletedJobs = completedJobs,
                        TrustLevel = trustLevel,
                        IsVerified = !string.IsNullOrEmpty(job.Employer?.AvatarUrl),
                        BudgetFitScore = budgetFitScore * 10,
                        FeedbackScore = feedbackScore,
                        MatchedSkills = matchedSkills
                    };
                }
            }

            if (bestMatchForThisEmployer != null)
            {
                matches.Add(bestMatchForThisEmployer);
            }
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
    
    public async Task<double> GetJobRatingAsync(Guid jobId, Guid reviewerId, Guid revieweeId)
    {
        var review = await _reviewRepository.GetReviewAsync(jobId, reviewerId, revieweeId);
        return review?.Rating ?? 0;
    }

    public async Task<object> GetEmployerRatingAsync(Guid jobId, Guid currentUserId, Guid employerId)
    {
        var avgRating = await GetUserRatingAsync(employerId);
        var score = await GetJobRatingAsync(jobId, currentUserId, employerId);

        return new
        {
            jobId = jobId,
            employerId = employerId,
            averageRating = avgRating,
            score = score
        };
    }

    public async Task<double> GetDistanceKmAsync(Guid jobId, Guid employerId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) throw new Exception("Job not found.");

        var employer = await _userRepository.GetByIdAsync(employerId);
        if (employer == null) throw new Exception("Employer not found.");

        double distance = CalculateDistance(job.Latitude, job.Longitude, employer.Latitude, employer.Longitude);
        return Math.Round(distance, 2);
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
