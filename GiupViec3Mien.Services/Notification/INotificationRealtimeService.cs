using GiupViec3Mien.Services.DTOs.Notification;

namespace GiupViec3Mien.Services.Notification;

public interface INotificationRealtimeService
{
    Task PushAsync(Guid recipientId, NotificationResponse notification);
}
