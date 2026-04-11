using Microsoft.EntityFrameworkCore;
using SkyBooker.Flights.API.Data;
using SkyBooker.Flights.API.Entities;

namespace SkyBooker.Flights.API.Repositories;

public class FlightRepository : IFlightRepository
{
    private readonly FlightDbContext _context;
    
    public FlightRepository(FlightDbContext context)
    {
        _context = context;
    }
    
    public async Task<Flight?> GetByIdAsync(int id)
    {
        return await _context.Flights.FindAsync(id);
    }
    
    public async Task<Flight?> GetByFlightNumberAsync(string flightNumber)
    {
        return await _context.Flights
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
    }
    
    public async Task<IEnumerable<Flight>> GetByOriginDestDateAsync(string origin, string dest, DateTime date)
    {
        return await _context.Flights
            .Where(f => f.OriginAirportCode == origin 
                        && f.DestinationAirportCode == dest
                        && f.DepartureTime.Date == date.Date
                        && f.Status != FlightStatus.Cancelled)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Flight>> GetByAirlineIdAsync(int airlineId)
    {
        return await _context.Flights
            .Where(f => f.AirlineId == airlineId)
            .OrderByDescending(f => f.DepartureTime)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Flight>> GetByStatusAsync(string status)
    {
        return await _context.Flights
            .Where(f => f.Status == status)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Flight>> GetAvailableFlightsAsync(string origin, string dest, DateTime date, int passengers)
    {
        return await _context.Flights
            .Where(f => f.OriginAirportCode == origin
                        && f.DestinationAirportCode == dest
                        && f.DepartureTime.Date == date.Date
                        && f.Status != FlightStatus.Cancelled
                        && f.AvailableSeats >= passengers)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();
    }
    
    public async Task<int> CountByAirlineIdAsync(int airlineId)
    {
        return await _context.Flights
            .CountAsync(f => f.AirlineId == airlineId);
    }
    
    public async Task<Flight> AddAsync(Flight flight)
    {
        flight.CreatedAt = DateTime.UtcNow;
        var entry = await _context.Flights.AddAsync(flight);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }
    
    public async Task UpdateAsync(Flight flight)
    {
        flight.UpdatedAt = DateTime.UtcNow;
        _context.Flights.Update(flight);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Flight flight)
    {
        _context.Flights.Remove(flight);
        await _context.SaveChangesAsync();
    }
    
    public async Task<int> DecrementSeatsAsync(int flightId, int count)
    {
        // Atomic operation - race condition proof
        return await _context.Flights
            .Where(f => f.FlightId == flightId && f.AvailableSeats >= count)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.AvailableSeats, f => f.AvailableSeats - count)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow));
    }
    
    public async Task<int> IncrementSeatsAsync(int flightId, int count)
    {
        return await _context.Flights
            .Where(f => f.FlightId == flightId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.AvailableSeats, f => f.AvailableSeats + count)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow));
    }
}