namespace SkyBooker.Flights.API.DTOs;
public class FlightRevenueDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int BookedSeats { get; set; }
    public double SeatUtilisationPercent { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, decimal> RevenueByClass { get; set; } = new();
}