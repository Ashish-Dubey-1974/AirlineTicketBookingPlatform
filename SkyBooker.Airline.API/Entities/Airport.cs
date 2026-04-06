using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyBooker.Airline.Entities;

/// <summary>
/// Represents a physical airport with IATA/ICAO codes and GPS coordinates.
/// IataCode is unique — enforced via EF Core index in AirlineDbContext.OnModelCreating.
/// Linked to Airline via AirlineAirport many-to-many join table.
/// </summary>
[Table("airports")]
public class Airport
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("airport_id")]
    public int AirportId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>3-letter IATA code, e.g. DEL, BOM, BLR</summary>
    [Required]
    [MaxLength(3)]
    [Column("iata_code")]
    public string IataCode { get; set; } = string.Empty;

    /// <summary>4-letter ICAO code, e.g. VIDP, VABB, VOBL</summary>
    [MaxLength(4)]
    [Column("icao_code")]
    public string? IcaoCode { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    [Column("country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>GPS latitude, e.g. 28.5562 for DEL</summary>
    [Column("latitude")]
    public double Latitude { get; set; }

    /// <summary>GPS longitude, e.g. 77.1000 for DEL</summary>
    [Column("longitude")]
    public double Longitude { get; set; }

    /// <summary>IANA timezone string, e.g. Asia/Kolkata</summary>
    [MaxLength(60)]
    [Column("timezone")]
    public string? Timezone { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation — many-to-many with Airline via AirlineAirport join table
    public ICollection<AirlineAirport> AirlineAirports { get; set; } = new List<AirlineAirport>();
}
