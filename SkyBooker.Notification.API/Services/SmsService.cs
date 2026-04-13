namespace SkyBooker.Notification.API.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        // Mock SMS sending - In production, use Twilio SDK
        _logger.LogInformation("📱 SMS SENT TO: {PhoneNumber}", phoneNumber);
        _logger.LogInformation("   Message: {Message}", message.Length > 100 ? message.Substring(0, 100) + "..." : message);
        
        await Task.Delay(50);
        return true;
    }

    public async Task<bool> SendBookingConfirmationSmsAsync(string phoneNumber, string pnrCode, string flightDetails)
    {
        var message = $"SkyBooker: Booking confirmed! PNR: {pnrCode}. {flightDetails} Download e-ticket from app. Thank you!";
        return await SendSmsAsync(phoneNumber, message);
    }
}