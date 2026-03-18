using GiupViec3Mien.Services.Interfaces;
using System.Threading.Tasks;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("email")]
public class NewsletterJob : IBackgroundJob
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public NewsletterJob(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public string JobId => "weekly-newsletter";
    public string CronExpression => "0 9 * * 1"; // Every Monday at 9:00 AM

    public async Task ExecuteAsync()
    {
        var users = await _userRepository.GetAllAsync();
        foreach (var user in users)
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendEmailAsync(
                    user.Email, 
                    "GiupViec3Mien Weekly Newsletter", 
                    $"Hi {user.FullName}, check out the new cleaning jobs available in your area this week!"
                );
            }
        }
    }
}
