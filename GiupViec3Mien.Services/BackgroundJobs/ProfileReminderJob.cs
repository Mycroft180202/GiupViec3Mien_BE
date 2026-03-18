using GiupViec3Mien.Services.Interfaces;
using System.Threading.Tasks;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("high-priority")]
public class ProfileReminderJob
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public ProfileReminderJob(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task SendReminderAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Role != Domain.Enums.Role.Worker || user.WorkerProfile == null) return;

        // Check if profile is still incomplete
        if (string.IsNullOrEmpty(user.WorkerProfile.Bio) || string.IsNullOrEmpty(user.WorkerProfile.Skills))
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Complete your profile to start working!",
                    $"Hi {user.FullName}, we noticed you haven't finished your profile yet. Completing it will help you get more job requests!"
                );
            }
        }
    }
}
