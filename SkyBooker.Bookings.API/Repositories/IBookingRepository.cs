using SkyBooker.Bookings.API.Entities;

namespace SkyBooker.Bookings.API.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(string bookingId);
    Task<Booking?> GetByPnrCodeAsync(string pnrCode);
    Task<IList<Booking>> GetByUserIdAsync(int userId);
    Task<IList<Booking>> GetByFlightIdAsync(int flightId);
    Task<IList<Booking>> GetByStatusAsync(string status);
    Task<IList<Booking>> GetUpcomingBookingsAsync(int userId);
    Task<IList<Booking>> GetPastBookingsAsync(int userId);
    Task<int> CountByFlightIdAndStatusAsync(int flightId, string status);
    Task<Booking> CreateAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
    Task<bool> DeleteAsync(string bookingId);
    Task<bool> ExistsByPnrCodeAsync(string pnrCode);
}