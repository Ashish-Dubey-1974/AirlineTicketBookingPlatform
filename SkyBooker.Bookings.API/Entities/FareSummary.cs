namespace SkyBooker.Bookings.API.Entities;

/// <summary>
/// Immutable fare breakdown record as per case study
/// </summary>
public record FareSummary
{
    public decimal BaseFare { get; init; }
    public decimal Gst { get; init; }      // 5% on BaseFare
    public decimal FuelSurcharge { get; init; }  // 10% on BaseFare
    public decimal BaggageCost { get; init; }    // extraKg * 200 INR
    public decimal MealCost { get; init; }       // 150 INR per passenger if selected
    public decimal TotalFare { get; init; }
    
    // Helper for creating from calculation
    public static FareSummary Create(decimal baseFare, int passengers, int extraBaggageKg, bool hasMeal)
    {
        var gst = baseFare * 0.05m;
        var fuel = baseFare * 0.10m;
        var baggage = extraBaggageKg * 200m;
        var meal = hasMeal ? 150m * passengers : 0;
        var total = baseFare + gst + fuel + baggage + meal;
        
        return new FareSummary
        {
            BaseFare = baseFare,
            Gst = gst,
            FuelSurcharge = fuel,
            BaggageCost = baggage,
            MealCost = meal,
            TotalFare = total
        };
    }
}