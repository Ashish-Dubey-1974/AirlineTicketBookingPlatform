using System.ComponentModel.DataAnnotations.Schema;

namespace SkyBooker.Airline.Entities;

/// <summary>
/// Many-to-many join table between Airline and Airport.
/// Represents which airports an airline operates from/to.
/// Composite PK: (AirlineId, AirportId) — defined in AirlineDbContext.OnModelCreating.
/// </summary>
[Table("airline_airports")]
public class AirlineAirport
{
    [Column("airline_id")]
    public int AirlineId { get; set; }

    [Column("airport_id")]
    public int AirportId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Airline Airline { get; set; } = null!;
    public Airport Airport { get; set; } = null!;
}
