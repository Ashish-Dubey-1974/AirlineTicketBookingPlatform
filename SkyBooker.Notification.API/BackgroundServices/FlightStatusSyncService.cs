using SkyBooker.Notification.API.Services;
namespace SkyBooker.Notification.API.BackgroundServices;

public class FlightStatusSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FlightStatusSyncService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(15);

    public FlightStatusSyncService(IServiceScopeFactory scopeFactory, ILogger<FlightStatusSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlightStatusSyncService started. Polling every {Interval} minutes", _pollInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                var flightClient = httpClientFactory.CreateClient("FlightService");
                
                // Get flights that need status sync (departing in next 3 hours)
                var now = DateTime.UtcNow;
                var threeHoursLater = now.AddHours(3);
                
                var flights = await flightClient.GetFromJsonAsync<List<FlightStatusInfo>>(
                    $"/api/flights/status-sync?from={now:yyyy-MM-ddTHH:mm:ss}&to={threeHoursLater:yyyy-MM-ddTHH:mm:ss}");
                
                if (flights != null)
                {
                    foreach (var flight in flights)
                    {
                        // In production: Call external airline API for real-time status
                        // var realStatus = await airlineApi.GetFlightStatus(flight.FlightNumber);
                        
                        // Mock status change simulation
                        var mockStatus = GetMockStatus(flight.CurrentStatus);
                        
                        if (mockStatus != flight.CurrentStatus)
                        {
                            // Update flight status
                            await flightClient.PutAsJsonAsync($"/api/flights/{flight.FlightId}/status", mockStatus);
                            
                            // Send notifications if status changed to DELAYED or CANCELLED
                            if (mockStatus == "DELAYED")
                            {
                                await notificationService.SendFlightDelayAlertAsync(flight.FlightId, 
                                    $"Flight {flight.FlightNumber} is delayed by 30 minutes.");
                            }
                            else if (mockStatus == "CANCELLED")
                            {
                                await notificationService.SendFlightCancellationAlertAsync(flight.FlightId, 
                                    $"Flight {flight.FlightNumber} has been cancelled. Please contact support.");
                            }
                            
                            _logger.LogInformation("Flight {FlightId} status updated from {OldStatus} to {NewStatus}", 
                                flight.FlightId, flight.CurrentStatus, mockStatus);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlightStatusSyncService");
            }
            
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private string GetMockStatus(string currentStatus)
    {
        var random = new Random();
        var rand = random.Next(100);
        
        // 5% chance to delay, 2% chance to cancel
        if (rand < 5 && currentStatus == "SCHEDULED")
            return "DELAYED";
        if (rand < 2 && currentStatus == "SCHEDULED")
            return "CANCELLED";
            
        return currentStatus;
    }
}

public class FlightStatusInfo
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
}