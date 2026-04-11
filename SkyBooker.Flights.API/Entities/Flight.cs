using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Flights.API.Entities;

[Table("flights")]
[Index(nameof(FlightNumber), IsUnique = true)]
[Index(nameof(OriginAirportCode), nameof(DestinationAirportCode), nameof(DepartureTime))]
public class Flight
{
    [Key]
    public int FlightId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string FlightNumber { get; set; } = string.Empty;
    
    public int AirlineId { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string OriginAirportCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(3)]
    public string DestinationAirportCode { get; set; } = string.Empty;
    
    public DateTime DepartureTime { get; set; }
    
    public DateTime ArrivalTime { get; set; }
    
    public int DurationMinutes { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "SCHEDULED";
    
    [MaxLength(50)]
    public string AircraftType { get; set; } = string.Empty;
    
    public int TotalSeats { get; set; }
    
    public int AvailableSeats { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

// Status constants
public static class FlightStatus
{
    public const string Scheduled = "SCHEDULED";
    public const string Delayed = "DELAYED";
    public const string Cancelled = "CANCELLED";
    public const string Departed = "DEPARTED";
    public const string Arrived = "ARRIVED";
}