using SkyBooker.Notification.API.Services;

namespace SkyBooker.Notification.API.BackgroundServices;

public class CheckInReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CheckInReminderService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromHours(1);

    public CheckInReminderService(IServiceScopeFactory scopeFactory, ILogger<CheckInReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CheckInReminderService started. Polling every {Interval} hours", _pollInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                
                var flightClient = httpClientFactory.CreateClient("FlightService");
                var bookingClient = httpClientFactory.CreateClient("BookingService");
                
                // Get flights departing in 24 hours
                var tomorrow = DateTime.UtcNow.AddHours(24);
                var dayAfter = tomorrow.AddHours(1);
                
                // Find flights with departure time between tomorrow and tomorrow+1 hour
                var flights = await flightClient.GetFromJsonAsync<List<FlightSchedule>>(
                    $"/api/flights/departing?from={tomorrow:yyyy-MM-ddTHH:mm:ss}&to={dayAfter:yyyy-MM-ddTHH:mm:ss}");
                
                if (flights != null)
                {
                    foreach (var flight in flights)
                    {
                        // Get bookings for this flight
                        var bookings = await bookingClient.GetFromJsonAsync<List<BookingInfo>>(
                            $"/api/bookings/flight/{flight.FlightId}");
                        
                        if (bookings != null)
                        {
                            foreach (var booking in bookings)
                            {
                                await notificationService.SendNotificationAsync(new DTOs.SendNotificationDto
                                {
                                    RecipientId = booking.UserId,
                                    Type = "CHECKIN_REMINDER",
                                    Title = $"Web Check-in Now Open - Flight {flight.FlightNumber}",
                                    Message = $"Your flight {flight.FlightNumber} from {flight.Origin} to {flight.Destination} departs in 24 hours. Please complete web check-in.",
                                    RelatedBookingId = booking.BookingId,
                                    Channel = "ALL"
                                });
                            }
                            
                            _logger.LogInformation("Check-in reminders sent for Flight {FlightId} to {Count} passengers", 
                                flight.FlightId, bookings.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckInReminderService");
            }
            
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}

public class FlightSchedule
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
}