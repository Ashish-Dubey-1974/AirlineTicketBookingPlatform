using MassTransit;
using SkyBooker.Bookings.API.Events;

namespace SkyBooker.Bookings.API.Consumers;

/// <summary>
/// MassTransit consumer for BookingConfirmedEvent
/// Notification Service will consume this event to send emails/SMS
/// </summary>
public class BookingConfirmedEventConsumer : IConsumer<BookingConfirmedEvent>
{
    private readonly ILogger<BookingConfirmedEventConsumer> _logger;

    public BookingConfirmedEventConsumer(ILogger<BookingConfirmedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "BookingConfirmedEvent received: BookingId={BookingId}, PNR={PnrCode}, UserId={UserId}",
            message.BookingId, message.PnrCode, message.UserId);
        
        // Notification Service will consume this and send:
        // 1. Email with e-ticket PDF attachment
        // 2. SMS with PNR code
        // 3. In-app notification
        
        await Task.CompletedTask;
    }
}