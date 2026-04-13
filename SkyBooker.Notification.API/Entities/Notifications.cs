using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Notification.API.Entities;

[Table("notifications")]
[Index(nameof(RecipientId))]
[Index(nameof(IsRead))]
[Index(nameof(SentAt))]
[Index(nameof(RelatedBookingId))]
public class Notifications
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Required]
    [Column("recipient_id")]
    public int RecipientId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("channel")]
    public string Channel { get; set; } = "APP";

    [MaxLength(36)]
    [Column("related_booking_id")]
    public string? RelatedBookingId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [Column("delivered_at")]
    public DateTime? DeliveredAt { get; set; }

    [MaxLength(500)]
    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}

public static class NotificationType
{
    public const string BookingConfirmed = "BOOKING_CONFIRMED";
    public const string FlightDelayed = "FLIGHT_DELAY";
    public const string FlightCancelled = "FLIGHT_CANCELLED";
    public const string GateChange = "GATE_CHANGE";
    public const string CheckInReminder = "CHECKIN_REMINDER";
    public const string BoardingReminder = "BOARDING_REMINDER";
    public const string PaymentReceived = "PAYMENT_RECEIVED";
}

public static class NotificationChannel
{
    public const string App = "APP";
    public const string Email = "EMAIL";
    public const string Sms = "SMS";
    public const string Push = "PUSH";
}