using SkyBooker.Notification.API.Entities;

namespace SkyBooker.Notification.API.Repositories;

public interface INotificationRepository
{
    Task<IList<Notifications>> FindByRecipientIdAsync(int recipientId);
    Task<IList<Notifications>> FindByRecipientIdAndIsReadAsync(int recipientId, bool isRead);
    Task<int> CountByRecipientIdAndIsReadAsync(int recipientId, bool isRead);
    Task<IList<Notifications>> FindByTypeAsync(string type);
    Task<IList<Notifications>> FindByRelatedBookingIdAsync(string bookingId);
    Task<Notifications> SaveAsync(Notifications notification);
    Task DeleteByNotificationIdAsync(int notificationId);
}