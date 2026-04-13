
using SkyBooker.Notification.API.DTOs;
namespace SkyBooker.Notification.API.BackgroundServices;

public class NoShowDetectionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NoShowDetectionService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(30);

    public NoShowDetectionService(IServiceScopeFactory scopeFactory, ILogger<NoShowDetectionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NoShowDetectionService started. Polling every {Interval} minutes", _pollInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                
                var flightClient = httpClientFactory.CreateClient("FlightService");
                var bookingClient = httpClientFactory.CreateClient("BookingService");
                var passengerClient = httpClientFactory.CreateClient("PassengerService");
                
                // Get flights that departed 1 hour ago
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);
                
                var flights = await flightClient.GetFromJsonAsync<List<FlightSchedule>>(
                    $"/api/flights/departed?before={oneHourAgo:yyyy-MM-ddTHH:mm:ss}");
                
                if (flights != null)
                {
                    foreach (var flight in flights)
                    {
                        // Get all bookings for this flight
                        var bookings = await bookingClient.GetFromJsonAsync<List<BookingInfo>>(
                            $"/api/bookings/flight/{flight.FlightId}");
                        
                        if (bookings != null)
                        {
                            foreach (var booking in bookings)
                            {
                                // Check if passengers checked in
                                var passengers = await passengerClient.GetFromJsonAsync<List<PassengerCheckInStatus>>(
                                    $"/api/passengers/booking/{booking.BookingId}/checkin-status");
                                
                                var hasCheckedIn = passengers?.Any(p => p.CheckedIn) ?? false;
                                
                                if (!hasCheckedIn)
                                {
                                    // Mark booking as NO_SHOW
                                    await bookingClient.PutAsJsonAsync($"/api/bookings/{booking.BookingId}/status", "NO_SHOW");
                                    _logger.LogWarning("Booking {BookingId} marked as NO_SHOW for Flight {FlightId}", 
                                        booking.BookingId, flight.FlightId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NoShowDetectionService");
            }
            
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}