using GiupViec3Mien.Services.Interfaces;
using GiupViec3Mien.Services.Messaging;
using MassTransit;
using System.Threading.Tasks;
using System;

namespace GiupViec3Mien.Services.Messaging.Consumers;

public class ChatConsumer : IConsumer<MessageSentEvent>
{
    private readonly IChatService _chatService;
    private readonly IUserRepository _userRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ChatConsumer(IChatService chatService, IUserRepository userRepository, IPublishEndpoint publishEndpoint)
    {
        _chatService = chatService;
        _userRepository = userRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<MessageSentEvent> context)
    {
        var msg = context.Message;

        // 1. Background DB Persistence
        await _chatService.SaveMessageAsync(msg.SenderId, msg.ReceiverId, msg.Message, msg.RoomId);

        // 2. Background Email Notification
        var receiver = await _userRepository.GetByIdAsync(msg.ReceiverId);
        var sender = await _userRepository.GetByIdAsync(msg.SenderId);
        
        if (receiver != null && !string.IsNullOrEmpty(receiver.Email))
        {
            string subject = $"New message from {sender?.FullName ?? "User"}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                    <h2 style='color: #2c3e50;'>New Chat Message</h2>
                    <p><strong>{sender?.FullName ?? "Someone"}</strong> sent you a message:</p>
                    <blockquote style='padding: 15px; background: #fdfdfd; border-left: 5px solid #3498db; font-style: italic;'>
                        ""{msg.Message}""
                    </blockquote>
                    <p>Reply directly in the app to keep the conversation going!</p>
                </div>";
            
            await _publishEndpoint.Publish(new SendEmailMessage(receiver.Email, subject, body));
        }

        // 3. Optional: Background Analytics
        await _publishEndpoint.Publish(new AnalyticsEvent("ChatMessageSent", msg.SenderId, $"RoomId: {msg.RoomId}"));
    }
}
