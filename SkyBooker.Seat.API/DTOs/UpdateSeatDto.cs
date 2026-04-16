namespace SkyBooker.Seat.API.DTOs;

public class UpdateSeatDto
{
    public string? SeatClass { get; set; }
    public decimal? PriceMultiplier { get; set; }
    public bool? IsWindow { get; set; }
    public bool? IsAisle { get; set; }
    public bool? HasExtraLegroom { get; set; }
}