// SkyBooker.Seat.API/Services/ISeatService.cs
using SkyBooker.Seat.API.DTOs;

namespace SkyBooker.Seat.API.Services;

public interface ISeatService
{
    Task AddSeatsForFlightAsync(int flightId, IList<CreateSeatDto> seats);
    Task<IList<SeatResponseDto>> GetSeatMapAsync(int flightId);
    Task<IList<SeatResponseDto>> GetAvailableSeatsAsync(int flightId);
    Task<SeatResponseDto?> GetSeatByIdAsync(int seatId);
    Task<SeatResponseDto> HoldSeatAsync(int seatId, int userId);
    Task ReleaseSeatAsync(int seatId);
    Task ConfirmSeatAsync(int seatId);
    Task<int> CountAvailableByClassAsync(int flightId, string seatClass);
    Task DeleteSeatsForFlightAsync(int flightId);
}