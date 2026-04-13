using SkyBooker.Passenger.API.Entities;

namespace SkyBooker.Passenger.API.Repositories;

public interface IPassengerRepository
{
    Task<PassengerInfo?> GetByIdAsync(int passengerId);
    Task<IList<PassengerInfo>> GetByBookingIdAsync(string bookingId);
    Task<PassengerInfo?> GetByPassportNumberAsync(string passportNumber);
    Task<PassengerInfo?> GetByTicketNumberAsync(string ticketNumber);
    Task<PassengerInfo?> GetBySeatIdAsync(int seatId);
    Task<int> CountByBookingIdAsync(string bookingId);
    Task<PassengerInfo> AddAsync(PassengerInfo passenger);
    Task<PassengerInfo> UpdateAsync(PassengerInfo passenger);
    Task<bool> DeleteByBookingIdAsync(string bookingId);
    Task<bool> DeleteByPassengerIdAsync(int passengerId);
    Task<IList<PassengerInfo>> GetCheckedInPassengersByFlightAsync(int flightId);
}