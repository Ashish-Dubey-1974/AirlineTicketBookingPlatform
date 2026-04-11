// SkyBooker.Seat.API/DTOs/CreateSeatDto.cs
using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Seat.API.DTOs;

public class CreateSeatDto
{
    [Required]
    [MaxLength(5)]
    public string SeatNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string SeatClass { get; set; } = "Economy";
    
    [Required]
    public int Row { get; set; }
    
    [Required]
    [MaxLength(1)]
    public string Column { get; set; } = string.Empty;
    
    public bool IsWindow { get; set; }
    public bool IsAisle { get; set; }
    public bool HasExtraLegroom { get; set; }
    
    [Range(0.5, 5.0)]
    public decimal PriceMultiplier { get; set; } = 1.0m;
}