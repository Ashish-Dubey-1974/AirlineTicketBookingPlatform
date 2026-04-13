namespace SkyBooker.Notification.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string attachmentName = "ticket.pdf")
    {
        // Mock email sending - In production, use MailKit or SendGrid
        _logger.LogInformation("📧 EMAIL SENT TO: {To}", to);
        _logger.LogInformation("   Subject: {Subject}", subject);
        _logger.LogInformation("   Body: {Body}", body?.Length > 200 ? body.Substring(0, 200) + "..." : body);
        
        if (attachment != null)
        {
            _logger.LogInformation("   Attachment: {AttachmentName} ({Size} bytes)", attachmentName, attachment.Length);
        }

        // Simulate network delay
        await Task.Delay(100);
        
        return true;
    }

    public async Task<bool> SendBookingConfirmationEmailAsync(string to, string pnrCode, string bookingId, string passengerNames, string flightDetails)
    {
        var subject = $"SkyBooker: Booking Confirmed - PNR {pnrCode}";
        var body = $@"
Dear Passenger,

Your booking with SkyBooker has been confirmed!

PNR Code: {pnrCode}
Booking ID: {bookingId}

Passenger Details:
{passengerNames}

Flight Details:
{flightDetails}

You can download your e-ticket from the SkyBooker app.

Thank you for choosing SkyBooker!

Safe Travels,
SkyBooker Team
";
        return await SendEmailAsync(to, subject, body);
    }
}