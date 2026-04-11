namespace SkyBooker.Bookings.API.Entities;

public static class BookingStatusConstants
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Cancelled = "CANCELLED";
    public const string Completed = "COMPLETED";
    public const string NoShow = "NO_SHOW";
    
    public static bool IsValidStatus(string status)
    {
        return status == Pending || status == Confirmed || 
               status == Cancelled || status == Completed || 
               status == NoShow;
    }
    
    public static bool CanTransitionFrom(string currentStatus, string newStatus)
    {
        return (currentStatus == Pending && newStatus == Confirmed) ||
               (currentStatus == Pending && newStatus == Cancelled) ||
               (currentStatus == Confirmed && newStatus == Cancelled) ||
               (currentStatus == Confirmed && newStatus == Completed) ||
               (currentStatus == Confirmed && newStatus == NoShow);
    }
}