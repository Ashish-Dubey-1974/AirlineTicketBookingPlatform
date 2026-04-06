namespace SkyBooker.Airline.DTOs;

public class AirportResponseDto
{
    public int AirportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Timezone { get; set; }
    public DateTime CreatedAt { get; set; }
}