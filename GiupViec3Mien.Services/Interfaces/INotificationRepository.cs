namespace GiupViec3Mien.Services.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(GiupViec3Mien.Domain.Entities.Notification notification);
    Task<GiupViec3Mien.Domain.Entities.Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<GiupViec3Mien.Domain.Entities.Notification>> GetByRecipientAsync(Guid recipientId, int limit = 20);
    Task<int> CountUnreadAsync(Guid recipientId);
    Task SaveChangesAsync();
}
