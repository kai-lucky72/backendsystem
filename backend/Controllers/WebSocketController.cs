using backend.DTOs.Notification;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/ws")]
public class WebSocketController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;

    public WebSocketController(INotificationService notificationService, IUserService userService)
    {
        _notificationService = notificationService;
        _userService = userService;
    }

    // Endpoint for sending private notifications via WebSocket
    [HttpPost("notification/private")]
    [Authorize]
    public async Task<IActionResult> SendPrivateNotification([FromBody] NotificationMessage message)
    {
        var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(senderId);
        var recipient = await _userService.GetUserByIdAsync(message.SenderId); // SenderId used as recipientId
        var title = "Notification";
        await _notificationService.SendCompleteNotificationAsync(
            sender,
            recipient,
            title,
            message.Message,
            false, // No email, just WebSocket and DB
            NotificationCategory.SYSTEM,
            NotificationPriority.MEDIUM
        );
        return Ok();
    }

    // Endpoint for sending broadcast notifications via WebSocket
    [HttpPost("notification/broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendBroadcastNotification([FromBody] NotificationMessage message)
    {
        var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(senderId);
        var title = "Notification";
        await _notificationService.SendCompleteNotificationToAllAsync(
            sender,
            title,
            message.Message,
            false, // No email, just WebSocket and DB
            NotificationCategory.SYSTEM,
            NotificationPriority.MEDIUM
        );
        return Ok();
    }
}