using GiupViec3Mien.Services.DTOs.Notification;

namespace GiupViec3Mien.Services.Notification;

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(Guid recipientId, string type, string title, string message, string? link = null);
    Task<IEnumerable<NotificationResponse>> GetRecentAsync(Guid recipientId, int limit = 20);
    Task<int> CountUnreadAsync(Guid recipientId);
    Task<bool> MarkAsReadAsync(Guid recipientId, Guid notificationId);
    Task<int> MarkAllAsReadAsync(Guid recipientId);
}
