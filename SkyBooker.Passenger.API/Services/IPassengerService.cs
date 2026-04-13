using SkyBooker.Passenger.API.DTOs;

namespace SkyBooker.Passenger.API.Services;

public interface IPassengerService
{
    Task<PassengerResponseDto> AddPassengerAsync(PassengerRequestDto dto);
    Task<PassengerResponseDto?> GetPassengerByIdAsync(int passengerId);
    Task<IList<PassengerResponseDto>> GetPassengersByBookingAsync(string bookingId);
    Task<PassengerResponseDto?> GetByPassportNumberAsync(string passportNumber);
    Task<PassengerResponseDto> UpdatePassengerAsync(int passengerId, PassengerRequestDto dto);
    Task<PassengerResponseDto> AssignSeatAsync(int passengerId, int seatId, string seatNumber);
    Task<string> GenerateTicketNumberAsync(string airlineCode, string flightNumber);
    Task<PassengerResponseDto> WebCheckInAsync(int passengerId, int newSeatId, string newSeatNumber);
    Task<bool> DeleteByBookingAsync(string bookingId);
    Task<int> GetPassengerCountByBookingAsync(string bookingId);
}
