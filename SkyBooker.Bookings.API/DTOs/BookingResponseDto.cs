namespace SkyBooker.Bookings.API.DTOs;

public class BookingResponseDto
{
    public string BookingId { get; set; } = string.Empty;
    public string PnrCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public int? ReturnFlightId { get; set; }
    public string TripType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalFare { get; set; }
    public decimal BaseFare { get; set; }
    public decimal Taxes { get; set; }
    public decimal AncillaryCharges { get; set; }
    public string? MealPreference { get; set; }
    public int LuggageKg { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? PaymentId { get; set; }
    public FlightInfoDto? Flight { get; set; }
    public FlightInfoDto? ReturnFlight { get; set; }
    public List<PassengerInfoDto> Passengers { get; set; } = new();
}

public class FlightInfoDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string AirlineName { get; set; } = string.Empty;
    public string OriginCode { get; set; } = string.Empty;
    public string OriginName { get; set; } = string.Empty;
    public string DestinationCode { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PassengerInfoDto
{
    public int PassengerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public DateTime PassportExpiry { get; set; }
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? TicketNumber { get; set; }
    public string PassengerType { get; set; } = string.Empty;
}