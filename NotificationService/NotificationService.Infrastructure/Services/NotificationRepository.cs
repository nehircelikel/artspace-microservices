using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Services;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationContext _context;

    public NotificationRepository(NotificationContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}