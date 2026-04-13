namespace SkyBooker.Notification.API.DTOs;

// This class is used in NoShowDetectionService and NotificationService
public class BookingInfo
{
    public string BookingId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
}

// Used in CheckInReminderService
public class FlightSchedule
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
}

// Used in NoShowDetectionService
public class PassengerCheckInStatus
{
    public int PassengerId { get; set; }
    public bool CheckedIn { get; set; }
}

// Used in FlightStatusSyncService
public class FlightStatusInfo
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
}

// Used in NotificationService
public class UserContact
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}