using Microsoft.EntityFrameworkCore;
using SkyBooker.Notification.API.Data;
using SkyBooker.Notification.API.DTOs;
using SkyBooker.Notification.API.Entities;
using SkyBooker.Notification.API.Consumers;

namespace SkyBooker.Notification.API.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<NotificationResponseDto> SendNotificationAsync(SendNotificationDto dto)
    {
        var channels = GetChannels(dto.Channel);
        
        var notification = new Notifications
        {
            RecipientId = dto.RecipientId,
            Type = dto.Type,
            Title = dto.Title,
            Message = dto.Message,
            RelatedBookingId = dto.RelatedBookingId,
            Channel = string.Join(",", channels),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        // Send to selected channels
        foreach (var channel in channels)
        {
            await SendViaChannel(channel, dto.RecipientId, dto.Title, dto.Message, dto.RelatedBookingId);
        }

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification sent to User {RecipientId}: {Title}", dto.RecipientId, dto.Title);
        
        return MapToResponse(notification);
    }

    public async Task<IList<NotificationResponseDto>> SendBulkNotificationAsync(SendBulkNotificationDto dto)
    {
        var results = new List<NotificationResponseDto>();
        
        foreach (var recipientId in dto.RecipientIds)
        {
            var result = await SendNotificationAsync(new SendNotificationDto
            {
                RecipientId = recipientId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                RelatedBookingId = dto.RelatedBookingId,
                Channel = "ALL"
            });
            results.Add(result);
        }
        
        _logger.LogInformation("Bulk notification sent to {Count} users", dto.RecipientIds.Count);
        return results;
    }

    public async Task SendBookingConfirmationAsync(int userId, string email, string phone, string bookingId, string pnrCode, int flightId, DateTime departureTime, List<PassengerInfo> passengers)
    {
        var passengerNames = string.Join(", ", passengers.Select(p => $"{p.Title} {p.FirstName} {p.LastName}"));
        var flightDetails = $"Flight {flightId} on {departureTime:dd-MM-yyyy HH:mm}";
        
        // 1. In-App Notification
        await SendNotificationAsync(new SendNotificationDto
        {
            RecipientId = userId,
            Type = NotificationType.BookingConfirmed,
            Title = "Booking Confirmed!",
            Message = $"Your booking with PNR {pnrCode} has been confirmed. {flightDetails}",
            RelatedBookingId = bookingId,
            Channel = "APP"
        });

        // 2. Email
        await _emailService.SendBookingConfirmationEmailAsync(email, pnrCode, bookingId, passengerNames, flightDetails);
        
        // 3. SMS
        await _smsService.SendBookingConfirmationSmsAsync(phone, pnrCode, flightDetails);
        
        _logger.LogInformation("Booking confirmation sent via all channels for PNR {PnrCode}", pnrCode);
    }

    public async Task SendFlightDelayAlertAsync(int flightId, string message)
    {
        // Get all passengers for this flight from Booking Service
        var bookingClient = _httpClientFactory.CreateClient("BookingService");
        var bookings = await bookingClient.GetFromJsonAsync<List<BookingInfo>>($"/api/bookings/flight/{flightId}");
        
        if (bookings == null) return;
        
        foreach (var booking in bookings)
        {
            await SendNotificationAsync(new SendNotificationDto
            {
                RecipientId = booking.UserId,
                Type = NotificationType.FlightDelayed,
                Title = $"Flight {flightId} Delayed",
                Message = message,
                RelatedBookingId = booking.BookingId,
                Channel = "ALL"
            });
        }
        
        _logger.LogWarning("Flight delay alert sent for Flight {FlightId}: {Message}", flightId, message);
    }

    public async Task SendFlightCancellationAlertAsync(int flightId, string message)
    {
        var bookingClient = _httpClientFactory.CreateClient("BookingService");
        var bookings = await bookingClient.GetFromJsonAsync<List<BookingInfo>>($"/api/bookings/flight/{flightId}");
        
        if (bookings == null) return;
        
        foreach (var booking in bookings)
        {
            await SendNotificationAsync(new SendNotificationDto
            {
                RecipientId = booking.UserId,
                Type = NotificationType.FlightCancelled,
                Title = $"Flight {flightId} Cancelled",
                Message = message,
                RelatedBookingId = booking.BookingId,
                Channel = "ALL"
            });
        }
        
        _logger.LogWarning("Flight cancellation alert sent for Flight {FlightId}", flightId);
    }

    public async Task SendGateChangeAlertAsync(int flightId, string newGate, string message)
    {
        var bookingClient = _httpClientFactory.CreateClient("BookingService");
        var bookings = await bookingClient.GetFromJsonAsync<List<BookingInfo>>($"/api/bookings/flight/{flightId}");
        
        if (bookings == null) return;
        
        foreach (var booking in bookings)
        {
            await SendNotificationAsync(new SendNotificationDto
            {
                RecipientId = booking.UserId,
                Type = NotificationType.GateChange,
                Title = $"Gate Change - Flight {flightId}",
                Message = $"{message} New gate: {newGate}",
                RelatedBookingId = booking.BookingId,
                Channel = "ALL"
            });
        }
    }

    public async Task<IList<NotificationResponseDto>> GetUserNotificationsAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
        
        return notifications.Select(MapToResponse).ToList();
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(int userId)
    {
        var count = await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);
        
        return new UnreadCountDto { UnreadCount = count };
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientId == userId);
        
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync();
        
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task DeleteNotificationAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientId == userId);
        
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    private List<string> GetChannels(string? channel)
    {
        return channel?.ToUpper() switch
        {
            "APP" => new List<string> { NotificationChannel.App },
            "EMAIL" => new List<string> { NotificationChannel.Email },
            "SMS" => new List<string> { NotificationChannel.Sms },
            "PUSH" => new List<string> { NotificationChannel.Push },
            "ALL" => new List<string> { NotificationChannel.App, NotificationChannel.Email, NotificationChannel.Sms },
            _ => new List<string> { NotificationChannel.App }
        };
    }

    private async Task SendViaChannel(string channel, int userId, string title, string message, string? bookingId)
    {
        // Get user contact from Auth Service
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var user = await authClient.GetFromJsonAsync<UserContact>($"/api/auth/users/{userId}/contact");
        
        if (user == null) return;
        
        switch (channel)
        {
            case NotificationChannel.Email:
                await _emailService.SendEmailAsync(user.Email, title, message);
                break;
            case NotificationChannel.Sms:
                await _smsService.SendSmsAsync(user.Phone, $"{title}: {message}");
                break;
            case NotificationChannel.App:
                // Already saved in DB
                break;
        }
    }

    private static NotificationResponseDto MapToResponse(Notifications n) => new()
    {
        NotificationId = n.NotificationId,
        RecipientId = n.RecipientId,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        Channel = n.Channel,
        RelatedBookingId = n.RelatedBookingId,
        IsRead = n.IsRead,
        SentAt = n.SentAt,
        DeliveredAt = n.DeliveredAt
    };
}

// Helper classes
public class BookingInfo
{
    public string BookingId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
}

public class UserContact
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}