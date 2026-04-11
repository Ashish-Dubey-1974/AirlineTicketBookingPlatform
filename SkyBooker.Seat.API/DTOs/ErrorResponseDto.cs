// SkyBooker.Seat.API/DTOs/ErrorResponseDto.cs
namespace SkyBooker.Seat.API.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}