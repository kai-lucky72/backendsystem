using backend.DTOs.Notification;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for handling WebSocket communication via HTTP endpoints
/// This provides REST endpoints that trigger WebSocket notifications
/// </summary>
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

    /// <summary>
    /// Endpoint for sending private notifications via WebSocket
    /// Matches Java's @MessageMapping("/ws.notification.private") functionality
    /// </summary>
    [HttpPost("notification/private")]
    [Authorize]
    public async Task<IActionResult> SendPrivateNotification([FromBody] NotificationMessage message)
    {
        try
        {
            var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var sender = await _userService.GetUserByIdAsync(senderId);
            
            // Extract target user from the message (matching Java logic exactly)
            var recipientId = message.SenderId; // Using sender field to hold recipient, same as Java
            
            if (recipientId > 0)
            {
                var recipient = await _userService.GetUserByIdAsync(recipientId);
                var title = "Notification";
                
                await _notificationService.SendCompleteNotificationAsync(
                    sender,
                    recipient,
                    title,
                    message.Message,
                    false, // Do not send an email, just WebSocket and database
                    Category.SYSTEM,
                    Priority.MEDIUM
                );
                
                return Ok(new { success = true, message = "Private notification sent successfully" });
            }
            else
            {
                return BadRequest("Invalid recipient ID");
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"Error sending private notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Endpoint for sending broadcast notifications via WebSocket
    /// Matches Java's @MessageMapping("/ws.notification.broadcast") functionality
    /// </summary>
    [HttpPost("notification/broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendBroadcastNotification([FromBody] NotificationMessage message)
    {
        try
        {
            var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var sender = await _userService.GetUserByIdAsync(senderId);
            
            var title = "Notification";
            
            await _notificationService.SendCompleteNotificationToAllAsync(
                sender,
                title,
                message.Message,
                false, // Do not send emails, just WebSocket and database
                Category.SYSTEM,
                Priority.MEDIUM
            );
            
            return Ok(new { success = true, message = "Broadcast notification sent successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error sending broadcast notification: {ex.Message}");
        }
    }
}

/// <summary>
/// Pure WebSocket Handler - matches Java's @MessageMapping functionality exactly
/// This handles direct WebSocket messages from clients (equivalent to Java's @MessageMapping)
/// </summary>
[Authorize]
public class WebSocketMessageHandler : Hub
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;

    public WebSocketMessageHandler(INotificationService notificationService, IUserService userService)
    {
        _notificationService = notificationService;
        _userService = userService;
    }

    /// <summary>
    /// Direct WebSocket message handler for private notifications
    /// Equivalent to Java's @MessageMapping("/ws.notification.private")
    /// Client sends message to this method directly via WebSocket
    /// </summary>
    public async Task SendPrivateNotification(NotificationMessage message)
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var senderId))
            {
                await Clients.Caller.SendAsync("Error", "User authentication failed");
                return;
            }

            var sender = await _userService.GetUserByIdAsync(senderId);
            
            // Extract target user from the message (matching Java logic exactly)
            var recipientId = message.SenderId; // Using sender field to hold recipient
            
            if (recipientId > 0)
            {
                var recipient = await _userService.GetUserByIdAsync(recipientId);
                var title = "Notification";
                
                await _notificationService.SendCompleteNotificationAsync(
                    sender,
                    recipient,
                    title,
                    message.Message,
                    false, // Do not send an email, just WebSocket and database
                    Category.SYSTEM,
                    Priority.MEDIUM
                );
                
                await Clients.Caller.SendAsync("NotificationSent", new { success = true, message = "Private notification sent" });
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Invalid recipient ID");
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send private notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Direct WebSocket message handler for broadcast notifications
    /// Equivalent to Java's @MessageMapping("/ws.notification.broadcast") with @PreAuthorize("hasRole('ADMIN')")
    /// Client sends message to this method directly via WebSocket
    /// </summary>
    public async Task SendBroadcastNotification(NotificationMessage message)
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var senderId))
            {
                await Clients.Caller.SendAsync("Error", "User authentication failed");
                return;
            }

            var sender = await _userService.GetUserByIdAsync(senderId);
            
            // Check if user is Admin (matching Java's @PreAuthorize("hasRole('ADMIN')"))
            if (sender.Role != Role.ADMIN)
            {
                await Clients.Caller.SendAsync("Error", "Insufficient permissions for broadcast");
                return;
            }
            
            var title = "Notification";
            
            await _notificationService.SendCompleteNotificationToAllAsync(
                sender,
                title,
                message.Message,
                false, // Do not send emails, just WebSocket and database
                Category.SYSTEM,
                Priority.MEDIUM
            );
            
            await Clients.Caller.SendAsync("NotificationSent", new { success = true, message = "Broadcast notification sent" });
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send broadcast notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Connection management - adds users to groups for targeted messaging
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userIdClaim}");
            
            // Get user role for role-based groups
            if (long.TryParse(userIdClaim, out var userId))
            {
                try
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{user.Role}");
                }
                catch
                {
                    // Silent fail for role group assignment
                }
            }
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Connection cleanup
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userIdClaim}");
            
            if (long.TryParse(userIdClaim, out var userId))
            {
                try
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Role_{user.Role}");
                }
                catch
                {
                    // Silent fail for role group cleanup
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }
}