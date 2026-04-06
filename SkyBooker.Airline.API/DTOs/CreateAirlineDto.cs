using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Airline.DTOs;
public class CreateAirlineDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z0-9]{2,3}$", ErrorMessage = "IataCode must be 2-3 uppercase letters/digits.")]
    public string IataCode { get; set; } = string.Empty;

    [MaxLength(4)]
    public string? IcaoCode { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(60)]
    public string? Country { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }
}

