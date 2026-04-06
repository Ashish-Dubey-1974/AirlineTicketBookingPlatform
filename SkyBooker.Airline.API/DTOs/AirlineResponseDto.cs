namespace SkyBooker.Airline.DTOs;

public class AirlineResponseDto
{
    public int AirlineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? LogoUrl { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
