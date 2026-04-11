using Microsoft.EntityFrameworkCore;
using SkyBooker.Bookings.API.Data;
using SkyBooker.Bookings.API.Entities;

namespace SkyBooker.Bookings.API.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(BookingDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Booking?> GetByIdAsync(string bookingId)
        => await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

    public async Task<Booking?> GetByPnrCodeAsync(string pnrCode)
        => await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.PnrCode == pnrCode.ToUpper());

    public async Task<IList<Booking>> GetByUserIdAsync(int userId)
        => await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();

    public async Task<IList<Booking>> GetByFlightIdAsync(int flightId)
        => await _context.Bookings
            .AsNoTracking()
            .Where(b => b.FlightId == flightId || b.ReturnFlightId == flightId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();

    public async Task<IList<Booking>> GetByStatusAsync(string status)
        => await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();

    public async Task<IList<Booking>> GetUpcomingBookingsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        
        // Need to join with Flight service for departure time
        // This is a simplified version - in production, you'd call Flight API
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Status == BookingStatusConstants.Confirmed)
            .OrderBy(b => b.BookedAt)
            .ToListAsync();
    }

    public async Task<IList<Booking>> GetPastBookingsAsync(int userId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId && 
                        (b.Status == BookingStatusConstants.Completed || 
                         b.Status == BookingStatusConstants.Cancelled ||
                         b.Status == BookingStatusConstants.NoShow))
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();
    }

    public async Task<int> CountByFlightIdAndStatusAsync(int flightId, string status)
        => await _context.Bookings
            .CountAsync(b => (b.FlightId == flightId || b.ReturnFlightId == flightId) 
                             && b.Status == status);

    public async Task<Booking> CreateAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Booking created: {BookingId} with PNR {PnrCode}", 
            booking.BookingId, booking.PnrCode);
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Booking updated: {BookingId}", booking.BookingId);
        return booking;
    }

    public async Task<bool> DeleteAsync(string bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) return false;
        
        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
        _logger.LogWarning("Booking deleted: {BookingId}", bookingId);
        return true;
    }

    public async Task<bool> ExistsByPnrCodeAsync(string pnrCode)
        => await _context.Bookings.AnyAsync(b => b.PnrCode == pnrCode.ToUpper());
}