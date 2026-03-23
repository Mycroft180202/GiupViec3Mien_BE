using GiupViec3Mien.Services.DTOs.Notification;
using GiupViec3Mien.Services.Interfaces;

namespace GiupViec3Mien.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationRealtimeService _notificationRealtimeService;

    public NotificationService(INotificationRepository notificationRepository, INotificationRealtimeService notificationRealtimeService)
    {
        _notificationRepository = notificationRepository;
        _notificationRealtimeService = notificationRealtimeService;
    }

    public async Task<NotificationResponse> CreateAsync(Guid recipientId, string type, string title, string message, string? link = null)
    {
        var notification = new Domain.Entities.Notification
        {
            RecipientId = recipientId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync();

        var response = Map(notification);
        await _notificationRealtimeService.PushAsync(recipientId, response);

        return response;
    }

    public async Task<IEnumerable<NotificationResponse>> GetRecentAsync(Guid recipientId, int limit = 20)
    {
        var notifications = await _notificationRepository.GetByRecipientAsync(recipientId, limit);
        return notifications.Select(Map);
    }

    public async Task<int> CountUnreadAsync(Guid recipientId)
    {
        return await _notificationRepository.CountUnreadAsync(recipientId);
    }

    public async Task<bool> MarkAsReadAsync(Guid recipientId, Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification == null || notification.RecipientId != recipientId)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _notificationRepository.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid recipientId)
    {
        var notifications = await _notificationRepository.GetByRecipientAsync(recipientId, 100);
        var changed = 0;

        foreach (var notification in notifications.Where(n => !n.IsRead))
        {
            notification.IsRead = true;
            changed++;
        }

        if (changed > 0)
        {
            await _notificationRepository.SaveChangesAsync();
        }

        return changed;
    }

    private static NotificationResponse Map(Domain.Entities.Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            Link = notification.Link,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
