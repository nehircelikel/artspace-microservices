using NotificationService.Core.Entities;

namespace NotificationService.Core.Interfaces;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<Notification> CreateAsync(Notification notification);
    Task MarkAsReadAsync(Guid id);
}