namespace SkyBooker.Notification.API.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string attachmentName = "ticket.pdf");
    Task<bool> SendBookingConfirmationEmailAsync(string to, string pnrCode, string bookingId, string passengerNames, string flightDetails);
}