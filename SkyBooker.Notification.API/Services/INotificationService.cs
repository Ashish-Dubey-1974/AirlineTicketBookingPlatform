using SkyBooker.Notification.API.DTOs;
using SkyBooker.Notification.API.Consumers;

namespace SkyBooker.Notification.API.Services;

public interface INotificationService
{
    // Single notification
    Task<NotificationResponseDto> SendNotificationAsync(SendNotificationDto dto);
    
    // Bulk notifications
    Task<IList<NotificationResponseDto>> SendBulkNotificationAsync(SendBulkNotificationDto dto);
    
    // Booking confirmation (multi-channel)
    Task SendBookingConfirmationAsync(int userId, string email, string phone, string bookingId, string pnrCode, int flightId, DateTime departureTime, List<PassengerInfo> passengers);
    
    // Flight alerts
    Task SendFlightDelayAlertAsync(int flightId, string message);
    Task SendFlightCancellationAlertAsync(int flightId, string message);
    Task SendGateChangeAlertAsync(int flightId, string newGate, string message);
    
    // User notifications
    Task<IList<NotificationResponseDto>> GetUserNotificationsAsync(int userId);
    Task<UnreadCountDto> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task DeleteNotificationAsync(int notificationId, int userId);
}