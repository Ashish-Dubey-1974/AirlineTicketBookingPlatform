using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Airline.DTOs;

public class CreateAirportDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "IataCode must be exactly 3 uppercase letters.")]
    public string IataCode { get; set; } = string.Empty;

    [MaxLength(4)]
    public string? IcaoCode { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Country { get; set; } = string.Empty;

    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    [MaxLength(60)]
    public string? Timezone { get; set; }
}

