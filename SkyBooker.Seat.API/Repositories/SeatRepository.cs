// SkyBooker.Seat.API/Repositories/SeatRepository.cs
using Microsoft.EntityFrameworkCore;
using SkyBooker.Seat.API.Data;
using SkyBooker.Seat.API.Entities;

namespace SkyBooker.Seat.API.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly SeatDbContext _context;
    
    public SeatRepository(SeatDbContext context)
    {
        _context = context;
    }
    
    public async Task<Seats?> GetByIdAsync(int seatId)
        => await _context.Seats.FindAsync(seatId);
    
    public async Task<IList<Seats>> GetByFlightIdAsync(int flightId)
        => await _context.Seats
            .Where(s => s.FlightId == flightId)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Column)
            .ToListAsync();
    
    public async Task<IList<Seats>> GetAvailableByFlightIdAsync(int flightId)
        => await _context.Seats
            .Where(s => s.FlightId == flightId && s.Status == "AVAILABLE")
            .OrderBy(s => s.Row).ThenBy(s => s.Column)
            .ToListAsync();
    
    public async Task<IList<Seats>> GetByFlightIdAndClassAsync(int flightId, string seatClass)
        => await _context.Seats
            .Where(s => s.FlightId == flightId && s.SeatClass == seatClass)
            .ToListAsync();
    
    public async Task<int> CountAvailableByClassAsync(int flightId, string seatClass)
        => await _context.Seats
            .CountAsync(s => s.FlightId == flightId && s.SeatClass == seatClass && s.Status == "AVAILABLE");
    
    public async Task<IList<Seats>> GetHeldBeforeAsync(DateTime expiryTime)
        => await _context.Seats
            .Where(s => s.Status == "HELD" && s.HeldSince < expiryTime)
            .ToListAsync();
    
    public async Task<Seats> AddAsync(Seats seat)
    {
        _context.Seats.Add(seat);
        await _context.SaveChangesAsync();
        return seat;
    }
    
    public async Task AddRangeAsync(IList<Seats> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(Seats seat)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteByFlightIdAsync(int flightId)
    {
        var seats = await _context.Seats.Where(s => s.FlightId == flightId).ToListAsync();
        _context.Seats.RemoveRange(seats);
        await _context.SaveChangesAsync();
    }
}