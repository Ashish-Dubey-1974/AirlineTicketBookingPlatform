// SkyBooker.Seat.API/Entities/Seat.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Seat.API.Entities;

[Table("seats")]
[Index(nameof(FlightId), nameof(SeatNumber), IsUnique = true)]
public class Seats
{
    [Key]
    public int SeatId { get; set; }
    
    public int FlightId { get; set; }
    
    [Required]
    [MaxLength(5)]
    public string SeatNumber { get; set; } = string.Empty; // e.g., 12A
    
    [MaxLength(20)]
    public string SeatClass { get; set; } = "Economy"; // Economy|Business|First
    
    public int Row { get; set; }
    
    [MaxLength(1)]
    public string Column { get; set; } = string.Empty;
    
    public bool IsWindow { get; set; }
    public bool IsAisle { get; set; }
    public bool HasExtraLegroom { get; set; }
    
    // CRITICAL: ConcurrencyToken for optimistic locking
    [ConcurrencyCheck]
    [MaxLength(20)]
    public string Status { get; set; } = "AVAILABLE"; // AVAILABLE|HELD|CONFIRMED|BLOCKED
    
    public DateTime? HeldSince { get; set; }
    public int? HeldByUserId { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal PriceMultiplier { get; set; } = 1.0m;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}