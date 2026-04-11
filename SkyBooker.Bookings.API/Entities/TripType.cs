namespace SkyBooker.Bookings.API.Entities;

public static class TripTypeConstants
{
    public const string OneWay = "ONE_WAY";
    public const string RoundTrip = "ROUND_TRIP";
    
    public static bool IsValid(string tripType)
    {
        return tripType == OneWay || tripType == RoundTrip;
    }
}