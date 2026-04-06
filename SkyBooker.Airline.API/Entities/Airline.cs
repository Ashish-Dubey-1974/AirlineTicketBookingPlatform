using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyBooker.Airline.Entities;

/// <summary>
/// Represents an airline registered on the SkyBooker platform.
/// IataCode is unique — enforced via EF Core index in AirlineDbContext.OnModelCreating.
/// </summary>
[Table("airlines")]
public class Airline
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("airline_id")]
    public int AirlineId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>2-letter IATA code, e.g. AI, 6E, SG</summary>
    [Required]
    [MaxLength(3)]
    [Column("iata_code")]
    public string IataCode { get; set; } = string.Empty;

    /// <summary>4-letter ICAO code, e.g. AIC, IGO, SEJ</summary>
    [MaxLength(4)]
    [Column("icao_code")]
    public string? IcaoCode { get; set; }

    [MaxLength(500)]
    [Column("logo_url")]
    public string? LogoUrl { get; set; }

    [MaxLength(60)]
    [Column("country")]
    public string? Country { get; set; }

    [MaxLength(150)]
    [EmailAddress]
    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    [Column("contact_phone")]
    public string? ContactPhone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation — many-to-many with Airport via AirlineAirport join table
    public ICollection<AirlineAirport> AirlineAirports { get; set; } = new List<AirlineAirport>();
}
