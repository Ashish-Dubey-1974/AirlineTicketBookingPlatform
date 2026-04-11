namespace SkyBooker.Flights.API.DTOs;

public class FlightSearchRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public int Passengers { get; set; } = 1;
    public string? Class { get; set; } // Economy, Business, First
    public string? Airline { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? DepartureTimeRange { get; set; } // Morning, Afternoon, Evening, Night
    public int? Stops { get; set; }
}

public class RoundTripSearchRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public int Passengers { get; set; } = 1;
    public string? Class { get; set; }
}

public class FlightResponse
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public int AirlineId { get; set; }
    public string AirlineName { get; set; } = string.Empty;
    public string AirlineLogo { get; set; } = string.Empty;
    public string OriginAirportCode { get; set; } = string.Empty;
    public string OriginAirportName { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationAirportCode { get; set; } = string.Empty;
    public string DestinationAirportName { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public string DurationDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public int AvailableSeats { get; set; }
    public decimal BasePrice { get; set; }
    public Dictionary<string, FareClassInfo> FareClasses { get; set; } = new();
}

public class FareClassInfo
{
    public decimal Price { get; set; }
    public int AvailableSeats { get; set; }
    public int BaggageAllowance { get; set; }
    public bool IsRefundable { get; set; }
    public int CancellationFeePercent { get; set; }
}