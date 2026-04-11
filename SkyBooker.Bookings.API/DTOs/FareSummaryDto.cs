namespace SkyBooker.Bookings.API.DTOs;

public class FareSummaryDto
{
    public decimal BaseFare { get; set; }
    public decimal Gst { get; set; }
    public decimal FuelSurcharge { get; set; }
    public decimal BaggageCost { get; set; }
    public decimal MealCost { get; set; }
    public decimal TotalFare { get; set; }
    public int FlightId { get; set; }
    public int PassengerCount { get; set; }
    public int ExtraBaggageKg { get; set; }
    public bool HasMeal { get; set; }
}