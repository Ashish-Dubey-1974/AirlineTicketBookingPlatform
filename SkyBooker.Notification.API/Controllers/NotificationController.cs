using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Notification.API.DTOs;
using SkyBooker.Notification.API.Services;

namespace SkyBooker.Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // GET /api/notifications - Get my notifications
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IList<NotificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value);
        return Ok(notifications);
    }

    // GET /api/notifications/unread-count - Get unread count
    [HttpGet("unread-count")]
    [Authorize]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(count);
    }

    // PUT /api/notifications/{id}/read - Mark as read
    [HttpPut("{id:int}/read")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        await _notificationService.MarkAsReadAsync(id, userId.Value);
        return Ok(new { message = "Notification marked as read" });
    }

    // PUT /api/notifications/mark-all-read - Mark all as read
    [HttpPut("mark-all-read")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        await _notificationService.MarkAllAsReadAsync(userId.Value);
        return Ok(new { message = "All notifications marked as read" });
    }

    // DELETE /api/notifications/{id} - Delete notification
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        await _notificationService.DeleteNotificationAsync(id, userId.Value);
        return NoContent();
    }

    // POST /api/notifications/send - Send notification (Admin only)
    [HttpPost("send")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(NotificationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
    {
        var result = await _notificationService.SendNotificationAsync(dto);
        return Ok(result);
    }

    // POST /api/notifications/send-bulk - Send bulk notification (Admin only)
    [HttpPost("send-bulk")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IList<NotificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendBulkNotification([FromBody] SendBulkNotificationDto dto)
    {
        var results = await _notificationService.SendBulkNotificationAsync(dto);
        return Ok(results);
    }

    // POST /api/notifications/flight-delay - Send flight delay alert (Staff only)
    [HttpPost("flight-delay")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendFlightDelayAlert([FromBody] SendFlightAlertDto dto)
    {
        await _notificationService.SendFlightDelayAlertAsync(dto.FlightId, dto.Message);
        return Ok(new { message = "Flight delay alerts sent" });
    }

    // POST /api/notifications/flight-cancellation - Send flight cancellation alert (Staff only)
    [HttpPost("flight-cancellation")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendFlightCancellationAlert([FromBody] SendFlightAlertDto dto)
    {
        await _notificationService.SendFlightCancellationAlertAsync(dto.FlightId, dto.Message);
        return Ok(new { message = "Flight cancellation alerts sent" });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}