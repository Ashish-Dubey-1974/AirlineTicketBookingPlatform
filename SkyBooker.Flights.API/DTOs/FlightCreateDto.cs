using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Flights.API.DTOs;

public class FlightCreateDto
{
    [Required]
    [MaxLength(10)]
    public string FlightNumber { get; set; } = string.Empty;
    
    [Required]
    public int AirlineId { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string OriginAirportCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(3)]
    public string DestinationAirportCode { get; set; } = string.Empty;
    
    [Required]
    public DateTime DepartureTime { get; set; }
    
    [Required]
    public DateTime ArrivalTime { get; set; }
    
    [MaxLength(50)]
    public string AircraftType { get; set; } = string.Empty;
    
    public int TotalSeats { get; set; }
    
    [Range(0, 1000000)]
    public decimal BasePrice { get; set; }
}

public class FlightUpdateDto
{
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public string? AircraftType { get; set; }
    public int? TotalSeats { get; set; }
    public decimal? BasePrice { get; set; }
}

public class FlightStatusUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}