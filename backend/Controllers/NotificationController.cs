using backend.DTOs.Notification;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly IAgentService _agentService;
    private readonly IManagerService _managerService;

    public NotificationController(INotificationService notificationService, IUserService userService, IAgentService agentService, IManagerService managerService)
    {
        _notificationService = notificationService;
        _userService = userService;
        _agentService = agentService;
        _managerService = managerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetMyNotifications()
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("User not found or account is inactive/deleted.");
            }
            var notifications = await _notificationService.GetNotificationsByRecipientAsync(user);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving notifications: {ex.Message}");
        }
    }

    [HttpGet("broadcast")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetBroadcastNotifications()
    {
        try
        {
            var notifications = await _notificationService.GetBroadcastNotificationsAsync();
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving broadcast notifications: {ex.Message}");
        }
    }

    [HttpPost("agent/{agentId}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<ActionResult<object>> SendToAgent(long agentId, [FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var sender = await _userService.GetUserByIdAsync(userId);
            if (sender == null)
            {
                return BadRequest("User not found or account is inactive/deleted.");
            }
            var agent = await _agentService.GetAgentByIdAsync(agentId);
            
            // Authorization check - matches Java logic exactly
            if (sender.Role == Role.MANAGER)
            {
                // For managers, check if the agent belongs to them
                if (agent.Manager.UserId != sender.Id)
                {
                    return Forbid("Agent does not belong to this manager");
                }
            }
            
            var title = "Notification";
            var category = Category.SYSTEM;
            var priority = Priority.MEDIUM;
            
            Notification notification;
            if (viaWebSocket)
                notification = await _notificationService.SendCompleteNotificationAsync(sender, agent.User, title, message, viaEmail, category, priority);
            else
                notification = await _notificationService.SendNotificationAsync(sender, agent.User, title, message, viaEmail, category, priority);
            
            return Ok(BuildNotificationResponse(notification, agent.User, sender));
        }
        catch (Exception ex)
        {
            return BadRequest($"Error sending notification to agent: {ex.Message}");
        }
    }

    [HttpPost("manager/{managerId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> SendToManager(long managerId, [FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var sender = await _userService.GetUserByIdAsync(userId);
            if (sender == null)
            {
                return BadRequest("User not found or account is inactive/deleted.");
            }
            var manager = await _managerService.GetManagerByIdAsync(managerId);
            
            var title = "Notification";
            var category = Category.SYSTEM;
            var priority = Priority.MEDIUM;
            
            Notification notification;
            if (viaWebSocket)
                notification = await _notificationService.SendCompleteNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
            else
                notification = await _notificationService.SendNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
            
            return Ok(BuildNotificationResponse(notification, manager.User, sender));
        }
        catch (Exception ex)
        {
            return BadRequest($"Error sending notification to manager: {ex.Message}");
        }
    }

    [HttpPost("broadcast")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> SendBroadcast([FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var sender = await _userService.GetUserByIdAsync(userId);
            if (sender == null)
            {
                return BadRequest("User not found or account is inactive/deleted.");
            }
            
            var title = "Notification";
            var category = Category.SYSTEM;
            var priority = Priority.MEDIUM;
            
            Notification notification;
            if (viaWebSocket)
                notification = await _notificationService.SendCompleteNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
            else
                notification = await _notificationService.SendNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
            
            return Ok(BuildNotificationResponse(notification, null, sender));
        }
        catch (Exception ex)
        {
            return BadRequest($"Error sending broadcast notification: {ex.Message}");
        }
    }

    [HttpPost("broadcast/managers")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<object>>> SendToAllManagers([FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found"));
            var sender = await _userService.GetUserByIdAsync(userId);
            if (sender == null)
            {
                return BadRequest("User not found or account is inactive/deleted.");
            }
            var managers = await _managerService.GetAllManagersAsync();
            
            var title = "Notification";
            var category = Category.SYSTEM;
            var priority = Priority.MEDIUM;
            var result = new List<object>();
            
            foreach (var manager in managers)
            {
                Notification notification;
                if (viaWebSocket)
                    notification = await _notificationService.SendCompleteNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
                else
                    notification = await _notificationService.SendNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
                
                result.Add(BuildNotificationResponse(notification, manager.User, sender));
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error sending notifications to managers: {ex.Message}");
        }
    }

    // Helper to build notification response structure (matching Java version exactly)
    private object BuildNotificationResponse(Notification notification, User? recipient, User sender)
    {
        // Build sender object - matches Java structure exactly
        var senderObj = new
        {
            role = sender.Role.ToString().ToLower(),
            workId = sender.WorkId ?? string.Empty,
            name = $"{sender.FirstName} {sender.LastName}"
        };

        // Build response - matches Java structure exactly
        return new
        {
            id = notification.Id,
            title = notification.Title ?? string.Empty,
            message = notification.Message ?? string.Empty,
            recipient = recipient?.WorkId ?? "All",
            priority = notification.Priority.ToString(),
            sentAt = notification.SentAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            sender = senderObj,
            status = notification.Status ?? "sent",
            // Removed: readBy, totalRecipients
            read = notification.ReadStatus
        };
    }

    // Keep the old method for backward compatibility if needed elsewhere
    private NotificationMessage MapToMessage(Notification n)
    {
        return new NotificationMessage
        {
            Id = n.Id,
            SenderId = n.Sender?.Id ?? 0,
            SenderWorkId = n.Sender?.WorkId ?? string.Empty,
            SenderName = n.Sender != null ? $"{n.Sender.FirstName} {n.Sender.LastName}" : string.Empty,
            Message = n.Message ?? string.Empty,
            Timestamp = n.SentAt,
            Type = n.Recipient == null ? "BROADCAST" : "DIRECT"
        };
    }

    private string FormatTimeAgo(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var timeSpan = now - dateTime;
        
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        else if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        else
            return $"{(int)timeSpan.TotalDays} days ago";
    }
}

// Enhanced NotificationHub to match Java WebSocket functionality exactly
[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;

    public NotificationHub(INotificationService notificationService, IUserService userService)
    {
        _notificationService = notificationService;
        _userService = userService;
    }

    public async Task SendNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }

    // Private notification (matching Java's @MessageMapping("/notification.private"))
    public async Task SendPrivateNotification(NotificationMessage message)
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "User authentication failed");
                return;
            }

            var sender = await _userService.GetUserByIdAsync(userId);
            if (sender == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found or account is inactive/deleted.");
                return;
            }
            
            var recipient = await _userService.GetUserByIdAsync(message.SenderId);
            if (recipient == null)
            {
                await Clients.Caller.SendAsync("Error", "Recipient not found or account is inactive/deleted.");
                return;
            }
            
            var category = Category.SYSTEM;
            var priority = Priority.MEDIUM;
            
            await _notificationService.SendCompleteNotificationAsync(sender, recipient, "Notification", message.Message, false, category, priority);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send private notification: {ex.Message}");
        }
    }

    // Broadcast notification (matching Java's @MessageMapping("/notification.broadcast"))
    public async Task SendBroadcastNotification(NotificationMessage message)
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "User authentication failed");
                return;
            }

            var sender = await _userService.GetUserByIdAsync(userId);
            if (sender == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found or account is inactive/deleted.");
                return;
            }
            
            // Check if user is Admin (matching Java's @PreAuthorize("hasRole('ADMIN')"))
            if (sender.Role != Role.ADMIN)
            {
                await Clients.Caller.SendAsync("Error", "Insufficient permissions for broadcast");
                return;
            }
            
            var category = Category.SYSTEM;
            var priority = Priority.MEDIUM;
            
            await _notificationService.SendCompleteNotificationToAllAsync(sender, "Notification", message.Message, false, category, priority);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send broadcast notification: {ex.Message}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userIdClaim}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userIdClaim}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}