using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Airline.DTOs;

public class UpdateAirlineDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

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

