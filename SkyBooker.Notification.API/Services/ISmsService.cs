namespace SkyBooker.Notification.API.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message);
    Task<bool> SendBookingConfirmationSmsAsync(string phoneNumber, string pnrCode, string flightDetails);
}