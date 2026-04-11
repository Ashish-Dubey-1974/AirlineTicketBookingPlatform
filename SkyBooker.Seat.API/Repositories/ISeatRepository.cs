// SkyBooker.Seat.API/Repositories/ISeatRepository.cs
using SkyBooker.Seat.API.Entities;

namespace SkyBooker.Seat.API.Repositories;

public interface ISeatRepository
{
    Task<Seats?> GetByIdAsync(int seatId);
    Task<IList<Seats>> GetByFlightIdAsync(int flightId);
    Task<IList<Seats>> GetAvailableByFlightIdAsync(int flightId);
    Task<IList<Seats>> GetByFlightIdAndClassAsync(int flightId, string seatClass);
    Task<int> CountAvailableByClassAsync(int flightId, string seatClass);
    Task<IList<Seats>> GetHeldBeforeAsync(DateTime expiryTime);
    Task<Seats> AddAsync(Seats seat);
    Task AddRangeAsync(IList<Seats> seats);
    Task UpdateAsync(Seats seat);
    Task DeleteByFlightIdAsync(int flightId);
}