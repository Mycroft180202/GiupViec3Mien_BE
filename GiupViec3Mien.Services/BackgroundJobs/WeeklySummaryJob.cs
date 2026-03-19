using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiupViec3Mien.Domain.Enums;
using GiupViec3Mien.Services.Interfaces;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

public class WeeklySummaryJob : IBackgroundJob
{
    private readonly IUserRepository _userRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public string JobId => "weekly-summary-report";
    public string CronExpression => Cron.Weekly(DayOfWeek.Monday, 8, 0);

    public WeeklySummaryJob(
        IUserRepository userRepository, 
        IJobRepository jobRepository, 
        IJobApplicationRepository applicationRepository,
        IBackgroundJobClient backgroundJobClient)
    {
        _userRepository = userRepository;
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task ExecuteAsync()
    {
        var lastWeek = DateTime.UtcNow.AddDays(-7);
        
        // 1. Get stats for the week
        var newJobs = await _jobRepository.GetCreatedSinceAsync(lastWeek);
        var newApplications = await _applicationRepository.GetCreatedSinceAsync(lastWeek);
        
        // 2. Get all users
        var users = await _userRepository.GetAllAsync();
        
        foreach (var user in users)
        {
            if (string.IsNullOrEmpty(user.Email)) continue;

            string report = "";
            if (user.Role == Role.Employer)
            {
                report = BuildEmployerReport(user.FullName, newApplications.Where(a => a.Job?.EmployerId == user.Id).ToList());
            }
            else if (user.Role == Role.Worker)
            {
                // Find jobs that match worker's general area or just show latest high-quality jobs
                var matchingJobs = newJobs
                    .Where(j => j.Status == JobStatus.Open)
                    .OrderByDescending(j => j.Price) // Highlight high paying jobs
                    .Take(5)
                    .ToList();
                    
                report = BuildWorkerReport(user.FullName, matchingJobs);
            }

            if (!string.IsNullOrEmpty(report))
            {
                _backgroundJobClient.Enqueue<SendEmailJob>(
                    job => job.SendAsync(user.Email, "GiúpViệc3Miền: Báo cáo hoạt động tuần qua", report)
                );
            }
        }
    }

    private string BuildEmployerReport(string fullName, List<Domain.Entities.JobApplication> apps)
    {
        if (apps.Count == 0) return "";

        var sb = new StringBuilder();
        sb.Append($"<h2>Chào {fullName}, đây là hoạt động tuần qua trên tin đăng của bạn!</h2>");
        sb.Append($"<p>Bạn đã nhận được <strong>{apps.Count}</strong> ứng tuyển mới.</p>");
        sb.Append("<ul>");
        foreach (var app in apps.Take(5))
        {
            sb.Append($"<li><strong>{app.Applicant?.FullName}</strong> đã ứng tuyển vào: <em>{app.Job?.Title}</em></li>");
        }
        sb.Append("</ul>");
        sb.Append("<p>Hãy đăng nhập để xem chi tiết và liên hệ với ứng viên nhé!</p>");
        return sb.ToString();
    }

    private string BuildWorkerReport(string fullName, List<Domain.Entities.Job> jobs)
    {
        if (jobs.Count == 0) return "";

        var sb = new StringBuilder();
        sb.Append($"<h2>Chào {fullName}, đây là các công việc mới phù hợp với bạn!</h2>");
        sb.Append($"<p>Tuần qua có <strong>{jobs.Count}</strong> công việc mới có thể bạn quan tâm:</p>");
        sb.Append("<ul>");
        foreach (var job in jobs)
        {
            sb.Append($"<li><strong>{job.Title}</strong> - Lương: {job.Price:N0} VNĐ - Địa điểm: {job.Location}</li>");
        }
        sb.Append("</ul>");
        sb.Append("<p>Mở ứng dụng ngay để ứng tuyển các công việc này nhé!</p>");
        return sb.ToString();
    }
}
