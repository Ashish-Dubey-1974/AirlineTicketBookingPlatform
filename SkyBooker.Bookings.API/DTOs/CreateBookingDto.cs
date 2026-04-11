using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Bookings.API.DTOs;

public class CreateBookingDto
{
    [Required]
    public int FlightId { get; set; }
    
    public int? ReturnFlightId { get; set; }
    
    [Required]
    [Range(1, 9)]
    public int PassengerCount { get; set; }
    
    [Required]
    [MaxLength(20)]
    [RegularExpression("ONE_WAY|ROUND_TRIP", ErrorMessage = "TripType must be ONE_WAY or ROUND_TRIP")]
    public string TripType { get; set; } = "ONE_WAY";
    
    [MaxLength(20)]
    public string? MealPreference { get; set; }
    
    [Range(0, 100)]
    public int ExtraBaggageKg { get; set; } = 0;
    
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;
    
    [Required]
    public List<PassengerSeatDto> Passengers { get; set; } = new();
}

public class PassengerSeatDto
{
    [Required]
    [MaxLength(10)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string PassportNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(60)]
    public string Nationality { get; set; } = string.Empty;
    
    [Required]
    public DateTime PassportExpiry { get; set; }
    
    [Required]
    public int SeatId { get; set; }
    
    [Required]
    [MaxLength(5)]
    public string SeatNumber { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string PassengerType { get; set; } = "ADULT";
}