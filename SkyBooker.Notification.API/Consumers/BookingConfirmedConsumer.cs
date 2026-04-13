using MassTransit;
using SkyBooker.Notification.API.DTOs;
using SkyBooker.Notification.API.Services;

namespace SkyBooker.Notification.API.Consumers;

public class BookingConfirmedEvent
{
    public string BookingId { get; set; } = string.Empty;
    public string PnrCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public int FlightId { get; set; }
    public int? ReturnFlightId { get; set; }
    public string TripType { get; set; } = string.Empty;
    public decimal TotalFare { get; set; }
    public DateTime DepartureTime { get; set; }
    public List<PassengerInfo> Passengers { get; set; } = new();
    public DateTime OccurredAt { get; set; }
}

public class PassengerInfo
{
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string PassengerType { get; set; } = string.Empty;
}

public class BookingConfirmedConsumer : IConsumer<BookingConfirmedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(INotificationService notificationService, ILogger<BookingConfirmedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing booking confirmation notification for BookingId: {BookingId}, PNR: {PnrCode}", 
            message.BookingId, message.PnrCode);

        // Send multi-channel notification
        await _notificationService.SendBookingConfirmationAsync(
            message.UserId,
            message.UserEmail,
            message.UserPhone,
            message.BookingId,
            message.PnrCode,
            message.FlightId,
            message.DepartureTime,
            message.Passengers);
    }
}