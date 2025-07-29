using backend.DTOs.Notification;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    public async Task<ActionResult<IEnumerable<NotificationMessage>>> GetMyNotifications()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _userService.GetUserByIdAsync(userId);
        var notifications = await _notificationService.GetNotificationsByRecipientAsync(user);
        var result = notifications.Select(MapToMessage).ToList();
        return Ok(result);
    }

    [HttpGet("broadcast")]
    public async Task<ActionResult<IEnumerable<NotificationMessage>>> GetBroadcastNotifications()
    {
        var notifications = await _notificationService.GetBroadcastNotificationsAsync();
        var result = notifications.Select(MapToMessage).ToList();
        return Ok(result);
    }

    [HttpPost("agent/{agentId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<NotificationMessage>> SendToAgent(long agentId, [FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(senderId);
        var agent = await _agentService.GetAgentByIdAsync(agentId);
        if (sender.Role == User.Role.Manager && agent.Manager.UserId != sender.Id)
            return Forbid();
        var title = "Notification";
        var category = Notification.Category.SYSTEM;
        var priority = Notification.Priority.MEDIUM;
        Notification notification;
        if (viaWebSocket)
            notification = await _notificationService.SendCompleteNotificationAsync(sender, agent.User, title, message, viaEmail, category, priority);
        else
            notification = await _notificationService.SendNotificationAsync(sender, agent.User, title, message, viaEmail, category, priority);
        return Ok(MapToMessage(notification));
    }

    [HttpPost("manager/{managerId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NotificationMessage>> SendToManager(long managerId, [FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(senderId);
        var manager = await _managerService.GetManagerByIdAsync(managerId);
        var title = "Notification";
        var category = Notification.Category.SYSTEM;
        var priority = Notification.Priority.MEDIUM;
        Notification notification;
        if (viaWebSocket)
            notification = await _notificationService.SendCompleteNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
        else
            notification = await _notificationService.SendNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
        return Ok(MapToMessage(notification));
    }

    [HttpPost("broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NotificationMessage>> SendBroadcast([FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(senderId);
        var title = "Notification";
        var category = Notification.Category.SYSTEM;
        var priority = Notification.Priority.MEDIUM;
        Notification notification;
        if (viaWebSocket)
            notification = await _notificationService.SendCompleteNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
        else
            notification = await _notificationService.SendNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
        return Ok(MapToMessage(notification));
    }

    [HttpPost("broadcast/managers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<NotificationMessage>>> SendToAllManagers([FromQuery] string message, [FromQuery] bool viaEmail = false, [FromQuery] bool viaWebSocket = true)
    {
        var senderId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(senderId);
        var managers = await _managerService.GetAllManagersAsync();
        var title = "Notification";
        var category = Notification.Category.SYSTEM;
        var priority = Notification.Priority.MEDIUM;
        var result = new List<NotificationMessage>();
        foreach (var manager in managers)
        {
            Notification notification;
            if (viaWebSocket)
                notification = await _notificationService.SendCompleteNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
            else
                notification = await _notificationService.SendNotificationAsync(sender, manager.User, title, message, viaEmail, category, priority);
            result.Add(MapToMessage(notification));
        }
        return Ok(result);
    }

    // Helper to map Notification to NotificationMessage DTO
    private NotificationMessage MapToMessage(Notification n)
    {
        return new NotificationMessage
        {
            Id = n.Id,
            SenderId = n.Sender?.Id ?? 0,
            SenderWorkId = n.Sender?.WorkId ?? string.Empty,
            SenderName = n.Sender != null ? $"{n.Sender.FirstName} {n.Sender.LastName}" : string.Empty,
            Message = n.Message,
            Timestamp = n.SentAt,
            Type = n.Recipient == null ? "BROADCAST" : "DIRECT"
        };
    }
}