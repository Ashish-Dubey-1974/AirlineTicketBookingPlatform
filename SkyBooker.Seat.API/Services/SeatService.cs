// SkyBooker.Seat.API/Services/SeatService.cs
using Microsoft.EntityFrameworkCore;
using SkyBooker.Seat.API.DTOs;
using SkyBooker.Seat.API.Entities;
using SkyBooker.Seat.API.Repositories;

namespace SkyBooker.Seat.API.Services;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _repo;
    private readonly ILogger<SeatService> _logger;
    
    public SeatService(ISeatRepository repo, ILogger<SeatService> logger)
    {
        _repo = repo;
        _logger = logger;
    }
    
    public async Task AddSeatsForFlightAsync(int flightId, IList<CreateSeatDto> seats)
    {
        var seatEntities = seats.Select(s => new Seats
        {
            FlightId = flightId,
            SeatNumber = s.SeatNumber,
            SeatClass = s.SeatClass,
            Row = s.Row,
            Column = s.Column,
            IsWindow = s.IsWindow,
            IsAisle = s.IsAisle,
            HasExtraLegroom = s.HasExtraLegroom,
            PriceMultiplier = s.PriceMultiplier,
            Status = "AVAILABLE"
        }).ToList();
        
        await _repo.AddRangeAsync(seatEntities);
        _logger.LogInformation("Added {Count} seats for Flight {FlightId}", seatEntities.Count, flightId);
    }
    
    public async Task<SeatResponseDto> HoldSeatAsync(int seatId, int userId)
    {
        var seats = await _repo.GetByIdAsync(seatId)
            ?? throw new KeyNotFoundException($"Seat {seatId} not found");
        
        if (seats.Status != "AVAILABLE")
            throw new InvalidOperationException($"Seat {seats.SeatNumber} is not available (status: {seats.Status})");
        
        seats.Status = "HELD";
        seats.HeldSince = DateTime.UtcNow;
        seats.HeldByUserId = userId;
        seats.UpdatedAt = DateTime.UtcNow;
        
        try
        {
            await _repo.UpdateAsync(seats);
            _logger.LogInformation("Seat {SeatNumber} held for User {UserId}", seats.SeatNumber, userId);
            return MapToResponse(seats);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while holding seat {SeatId}", seatId);
            throw new InvalidOperationException("Seat was taken by another user. Please try another seat.");
        }
    }
    
    public async Task ReleaseSeatAsync(int seatId)
    {
        var seat = await _repo.GetByIdAsync(seatId);
        if (seat != null && seat.Status == "HELD")
        {
            seat.Status = "AVAILABLE";
            seat.HeldSince = null;
            seat.HeldByUserId = null;
            seat.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(seat);
            _logger.LogInformation("Seat {SeatNumber} released", seat.SeatNumber);
        }
    }
    
    public async Task ConfirmSeatAsync(int seatId)
    {
        var seat = await _repo.GetByIdAsync(seatId);
        if (seat != null && (seat.Status == "AVAILABLE" || seat.Status == "HELD"))
        {
            seat.Status = "CONFIRMED";
            seat.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(seat);
            _logger.LogInformation("Seat {SeatNumber} confirmed", seat.SeatNumber);
        }
    }
    
    public async Task<IList<SeatResponseDto>> GetSeatMapAsync(int flightId)
    {
        var seats = await _repo.GetByFlightIdAsync(flightId);
        return seats.Select(MapToResponse).ToList();
    }
    
    public async Task<IList<SeatResponseDto>> GetAvailableSeatsAsync(int flightId)
    {
        var seats = await _repo.GetAvailableByFlightIdAsync(flightId);
        return seats.Select(MapToResponse).ToList();
    }
    
    public async Task<SeatResponseDto?> GetSeatByIdAsync(int seatId)
    {
        var seat = await _repo.GetByIdAsync(seatId);
        return seat == null ? null : MapToResponse(seat);
    }
    
    public async Task<int> CountAvailableByClassAsync(int flightId, string seatClass)
        => await _repo.CountAvailableByClassAsync(flightId, seatClass);
    
    public async Task DeleteSeatsForFlightAsync(int flightId)
    {
        await _repo.DeleteByFlightIdAsync(flightId);
        _logger.LogInformation("Deleted all seats for Flight {FlightId}", flightId);
    }
    
    private static SeatResponseDto MapToResponse(Seats s) => new()
    {
        SeatId = s.SeatId,
        FlightId = s.FlightId,
        SeatNumber = s.SeatNumber,
        SeatClass = s.SeatClass,
        Row = s.Row,
        Column = s.Column,
        IsWindow = s.IsWindow,
        IsAisle = s.IsAisle,
        HasExtraLegroom = s.HasExtraLegroom,
        Status = s.Status,
        PriceMultiplier = s.PriceMultiplier
    };
}