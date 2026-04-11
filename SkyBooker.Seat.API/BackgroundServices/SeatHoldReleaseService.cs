// SkyBooker.Seat.API/BackgroundServices/SeatHoldReleaseService.cs
using SkyBooker.Seat.API.Repositories;
using SkyBooker.Seat.API.Services;

namespace SkyBooker.Seat.API.BackgroundServices;

public class SeatHoldReleaseService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatHoldReleaseService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _holdExpiry = TimeSpan.FromMinutes(15);
    
    public SeatHoldReleaseService(IServiceScopeFactory scopeFactory, ILogger<SeatHoldReleaseService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SeatHoldReleaseService started. Polling every {Interval} minutes.", _pollInterval.TotalMinutes);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var seatRepo = scope.ServiceProvider.GetRequiredService<ISeatRepository>();
                var seatService = scope.ServiceProvider.GetRequiredService<ISeatService>();
                
                var expiryTime = DateTime.UtcNow.Subtract(_holdExpiry);
                var expiredSeats = await seatRepo.GetHeldBeforeAsync(expiryTime);
                
                foreach (var seat in expiredSeats)
                {
                    await seatService.ReleaseSeatAsync(seat.SeatId);
                }
                
                if (expiredSeats.Any())
                    _logger.LogInformation("Released {Count} expired seat holds", expiredSeats.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SeatHoldReleaseService");
            }
            
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}