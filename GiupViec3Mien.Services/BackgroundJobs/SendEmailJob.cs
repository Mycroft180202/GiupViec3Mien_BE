using GiupViec3Mien.Services.Interfaces;
using System.Threading.Tasks;
using System.ComponentModel;
using Hangfire;

namespace GiupViec3Mien.Services.BackgroundJobs;

[Queue("email")]
public class SendEmailJob
{
    private readonly IEmailService _emailService;

    public SendEmailJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [DisplayName("Send Email to {0}: {1}")]
    public async Task SendAsync(string to, string subject, string body)
    {
        await _emailService.SendEmailAsync(to, subject, body);
    }
}
