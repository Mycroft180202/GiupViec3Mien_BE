using GiupViec3Mien.Services.DTOs.Notification;
using GiupViec3Mien.Services.Notification;
using Microsoft.AspNetCore.SignalR;

namespace GiupViec3Mien.Presentation.Hubs;

public class SignalRNotificationRealtimeService : INotificationRealtimeService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimeService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushAsync(Guid recipientId, NotificationResponse notification)
    {
        await _hubContext.Clients.User(recipientId.ToString()).SendAsync("NotificationReceived", notification);
    }
}
