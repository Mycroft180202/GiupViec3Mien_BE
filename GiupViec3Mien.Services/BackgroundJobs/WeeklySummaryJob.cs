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
        // Weekly summary emails are currently disabled.
        /*
        var lastWeek = DateTime.UtcNow.AddDays(-7);
        ...
        */
        await Task.CompletedTask;
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
