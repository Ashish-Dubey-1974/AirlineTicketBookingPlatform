using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Notification.API.DTOs;

public class SendNotificationDto
{
    [Required]
    public int RecipientId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(36)]
    public string? RelatedBookingId { get; set; }

    [MaxLength(20)]
    public string? Channel { get; set; }  // APP, EMAIL, SMS, ALL
}

public class SendBulkNotificationDto
{
    [Required]
    public List<int> RecipientIds { get; set; } = new();

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(36)]
    public string? RelatedBookingId { get; set; }
}

public class SendFlightAlertDto
{
    [Required]
    public int FlightId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AlertType { get; set; } = string.Empty;  // DELAY, CANCELLATION, GATE_CHANGE

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
}

public class SendMyNotificationDto
{
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(36)]
    public string? RelatedBookingId { get; set; }
}
