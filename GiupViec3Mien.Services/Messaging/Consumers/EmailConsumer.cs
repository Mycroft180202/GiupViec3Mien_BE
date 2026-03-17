using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System.Threading.Tasks;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class EmailConsumer : IConsumer<SendEmailMessage>
{
    private readonly IEmailService _emailService;

    public EmailConsumer(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<SendEmailMessage> context)
    {
        var msg = context.Message;
        await _emailService.SendEmailAsync(msg.To, msg.Subject, msg.Body);
    }
}
