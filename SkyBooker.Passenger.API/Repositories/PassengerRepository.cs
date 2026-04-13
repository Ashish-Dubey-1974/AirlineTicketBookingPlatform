using Microsoft.EntityFrameworkCore;
using SkyBooker.Passenger.API.Data;
using SkyBooker.Passenger.API.Entities;

namespace SkyBooker.Passenger.API.Repositories;

public class PassengerRepository : IPassengerRepository
{
    private readonly PassengerDbContext _context;
    private readonly ILogger<PassengerRepository> _logger;

    public PassengerRepository(PassengerDbContext context, ILogger<PassengerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PassengerInfo?> GetByIdAsync(int passengerId)
        => await _context.Passengers.FindAsync(passengerId);

    public async Task<IList<PassengerInfo>> GetByBookingIdAsync(string bookingId)
        => await _context.Passengers
            .Where(p => p.BookingId == bookingId)
            .OrderBy(p => p.PassengerId)
            .ToListAsync();

    public async Task<PassengerInfo?> GetByPassportNumberAsync(string passportNumber)
        => await _context.Passengers
            .FirstOrDefaultAsync(p => p.PassportNumber == passportNumber.ToUpper());

    public async Task<PassengerInfo?> GetByTicketNumberAsync(string ticketNumber)
        => await _context.Passengers
            .FirstOrDefaultAsync(p => p.TicketNumber == ticketNumber);

    public async Task<PassengerInfo?> GetBySeatIdAsync(int seatId)
        => await _context.Passengers
            .FirstOrDefaultAsync(p => p.SeatId == seatId);

    public async Task<int> CountByBookingIdAsync(string bookingId)
        => await _context.Passengers
            .CountAsync(p => p.BookingId == bookingId);

    public async Task<PassengerInfo> AddAsync(PassengerInfo passenger)
    {
        _context.Passengers.Add(passenger);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Passenger added: {FirstName} {LastName} for Booking {BookingId}", 
            passenger.FirstName, passenger.LastName, passenger.BookingId);
        return passenger;
    }

    public async Task<PassengerInfo> UpdateAsync(PassengerInfo passenger)
    {
        passenger.UpdatedAt = DateTime.UtcNow;
        _context.Passengers.Update(passenger);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Passenger updated: PassengerId={PassengerId}", passenger.PassengerId);
        return passenger;
    }

    public async Task<bool> DeleteByBookingIdAsync(string bookingId)
    {
        var passengers = await _context.Passengers.Where(p => p.BookingId == bookingId).ToListAsync();
        if (!passengers.Any()) return false;
        
        _context.Passengers.RemoveRange(passengers);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted {Count} passengers for Booking {BookingId}", passengers.Count, bookingId);
        return true;
    }

    public async Task<bool> DeleteByPassengerIdAsync(int passengerId)
    {
        var passenger = await _context.Passengers.FindAsync(passengerId);
        if (passenger == null) return false;
        
        _context.Passengers.Remove(passenger);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IList<PassengerInfo>> GetCheckedInPassengersByFlightAsync(int flightId)
    {
        // This would need a join with Bookings - simplified for now
        return await _context.Passengers
            .Where(p => p.CheckedIn == true)
            .ToListAsync();
    }
}