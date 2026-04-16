using Microsoft.EntityFrameworkCore;
using SkyBooker.Notification.API.Data;
using SkyBooker.Notification.API.Entities;

namespace SkyBooker.Notification.API.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context) => _context = context;

    public async Task<IList<Notifications>> FindByRecipientIdAsync(int recipientId) =>
        await _context.Notifications.Where(n => n.RecipientId == recipientId)
                                    .OrderByDescending(n => n.SentAt).ToListAsync();

    public async Task<IList<Notifications>> FindByRecipientIdAndIsReadAsync(int recipientId, bool isRead) =>
        await _context.Notifications.Where(n => n.RecipientId == recipientId && n.IsRead == isRead).ToListAsync();

    public async Task<int> CountByRecipientIdAndIsReadAsync(int recipientId, bool isRead) =>
        await _context.Notifications.CountAsync(n => n.RecipientId == recipientId && n.IsRead == isRead);

    public async Task<IList<Notifications>> FindByTypeAsync(string type) =>
        await _context.Notifications.Where(n => n.Type == type).ToListAsync();

    public async Task<IList<Notifications>> FindByRelatedBookingIdAsync(string bookingId) =>
        await _context.Notifications.Where(n => n.RelatedBookingId == bookingId).ToListAsync();

    public async Task<Notifications> SaveAsync(Notifications notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task DeleteByNotificationIdAsync(int notificationId)
    {
        var n = await _context.Notifications.FindAsync(notificationId);
        if (n != null) { _context.Notifications.Remove(n); await _context.SaveChangesAsync(); }
    }
}