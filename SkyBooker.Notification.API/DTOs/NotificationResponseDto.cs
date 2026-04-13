namespace SkyBooker.Notification.API.DTOs;

public class NotificationResponseDto
{
    public int NotificationId { get; set; }
    public int RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? RelatedBookingId { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class UnreadCountDto
{
    public int UnreadCount { get; set; }
}