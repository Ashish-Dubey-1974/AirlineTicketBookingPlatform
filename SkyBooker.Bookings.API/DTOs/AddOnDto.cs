using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Bookings.API.DTOs;

public class AddOnDto
{
    [Required]
    public string BookingId { get; set; } = string.Empty;
    
    [Range(0, 100)]
    public int? ExtraBaggageKg { get; set; }
    
    [MaxLength(20)]
    public string? MealPreference { get; set; }
}