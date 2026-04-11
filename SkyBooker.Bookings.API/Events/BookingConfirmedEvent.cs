namespace SkyBooker.Bookings.API.Events;

/// <summary>
/// Published via MassTransit/RabbitMQ when booking is confirmed
/// Consumer: Notification Service
/// </summary>
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
    public List<PassengerInfoEvent> Passengers { get; set; } = new();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class PassengerInfoEvent
{
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string PassengerType { get; set; } = string.Empty;
}