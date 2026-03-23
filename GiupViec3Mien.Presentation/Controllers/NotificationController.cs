using System.Security.Claims;
using GiupViec3Mien.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiupViec3Mien.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 20)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notifications = await _notificationService.GetRecentAsync(userId, limit);
        var unreadCount = await _notificationService.CountUnreadAsync(userId);

        return Ok(new { items = notifications, unreadCount });
    }

    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await _notificationService.MarkAsReadAsync(userId, notificationId);
        if (!success)
        {
            return NotFound(new { message = "Notification not found." });
        }

        return Ok(new { message = "Notification marked as read." });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var count = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { count });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out userId);
    }
}
