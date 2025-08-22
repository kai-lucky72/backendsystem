using backend.DTOs.Notification;
using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserRepository _userRepository;

    public NotificationService(
        ILogger<NotificationService> logger,
        INotificationRepository notificationRepository,
        IAuditLogService auditLogService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
        _auditLogService = auditLogService;
        _userRepository = userRepository;
    }

    public async Task<Notification> SendNotificationAsync(User sender, User recipient, string title, string message, bool viaEmail, Category category, Priority priority)
    {
        var notification = new Notification
        {
            SenderId = sender.Id,
            RecipientId = recipient.Id,
            Title = title,
            Message = message,
            SentAt = DateTime.Now,
            ViaEmail = viaEmail,
            Category = category,
            Priority = priority,
            Status = "sent"
        };
        
        var savedNotification = await _notificationRepository.AddAsync(notification);
        
        await _auditLogService.LogEventAsync(
                "SEND_NOTIFICATION",
                "NOTIFICATION",
                savedNotification.Id.ToString(),
                sender,
                $"Notification sent to: {recipient.Email}"
        );
        
        return savedNotification;
    }

    public async Task<Notification> SendNotificationToAllAsync(User sender, string title, string message, bool viaEmail, Category category, Priority priority)
    {
        var notification = new Notification
        {
            SenderId = sender.Id,
            RecipientId = null, // null for broadcast
            Title = title,
            Message = message,
            SentAt = DateTime.Now,
            ViaEmail = viaEmail,
            Category = category,
            Priority = priority,
            Status = "sent"
        };
        
        var savedNotification = await _notificationRepository.AddAsync(notification);
        
        await _auditLogService.LogEventAsync(
                "SEND_BROADCAST_NOTIFICATION",
                "NOTIFICATION",
                savedNotification.Id.ToString(),
                sender,
                "Broadcast notification sent to all users"
        );
        
        return savedNotification;
    }

    public async Task<IEnumerable<Notification>> GetNotificationsBySenderAsync(User sender)
    {
        return await _notificationRepository.GetBySenderOrderBySentAtDescAsync(sender);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsByRecipientAsync(User recipient)
    {
        return await _notificationRepository.GetByRecipientOrderBySentAtDescAsync(recipient);
    }

    public async Task<IEnumerable<Notification>> GetBroadcastNotificationsAsync()
    {
        return await _notificationRepository.GetByRecipientIsNullOrderBySentAtDescAsync();
    }
    
    public Task SendWebSocketNotificationAsync(User sender, User recipient, string message)
    {
        var notification = CreateNotificationMessage(sender, message, "DIRECT");
        
        // In a real implementation, you would use SignalR or similar WebSocket framework
        // For now, we'll just log the notification
        _logger.LogInformation("WebSocket notification sent to {Recipient}: {Message}", recipient.WorkId, message);
        
        // Send to specific user (implementation would depend on your WebSocket setup)
        // messagingTemplate.convertAndSendToUser(
        //     recipient.WorkId,  // Username for STOMP subscription
        //     "/queue/notifications", // User-specific destination prefix
        //     notification           // Payload
        // );
        
        return Task.CompletedTask;
    }

    public Task SendWebSocketNotificationToAllAsync(User sender, string message)
    {
        var notification = CreateNotificationMessage(sender, message, "BROADCAST");
        
        // In a real implementation, you would use SignalR or similar WebSocket framework
        // For now, we'll just log the notification
        _logger.LogInformation("WebSocket broadcast notification sent: {Message}", message);
        
        // Send to all connected users
        // messagingTemplate.convertAndSend("/topic/notifications", notification);
        
        return Task.CompletedTask;
    }

    public async Task<Notification> SendCompleteNotificationAsync(User sender, User recipient, string title, string message, bool viaEmail, Category category, Priority priority)
    {
        var notification = await SendNotificationAsync(sender, recipient, title, message, viaEmail, category, priority);
        await SendWebSocketNotificationAsync(sender, recipient, message);
        return notification;
    }

    public async Task<Notification> SendCompleteNotificationToAllAsync(User sender, string title, string message, bool viaEmail, Category category, Priority priority)
    {
        var notification = await SendNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
        await SendWebSocketNotificationToAllAsync(sender, message);
        return notification;
    }

    public async Task<Notification> SendNotificationAsync(Dictionary<string, string> body, User sender)
    {
        // Extract notification details from the body
        var title = body.GetValueOrDefault("title", "System Notification");
        var message = body.GetValueOrDefault("message", "");
        var recipientId = body.GetValueOrDefault("recipientId", "");
        var viaEmail = bool.Parse(body.GetValueOrDefault("viaEmail", "false"));
        
        if (!Enum.TryParse(body.GetValueOrDefault("category", "System").ToUpper(), out Category category))
            throw new ArgumentException("Invalid category");
        var priorityString = body.GetValueOrDefault("priority", "Medium").ToLower();
        if (priorityString == "normal") priorityString = "medium";
        if (!Enum.TryParse(priorityString.ToUpper(), out Priority priority))
            throw new ArgumentException("Invalid Priority");
            
        if (string.IsNullOrEmpty(recipientId))
        {
            // Broadcast notification
            return await SendNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
        }
        else
        {
            // Direct notification
            var recipient = await _userRepository.GetByIdAsync(long.Parse(recipientId));
            return await SendNotificationAsync(sender, recipient ?? throw new InvalidOperationException(), title, message, viaEmail, category, priority);
        }
    }

    public async Task<PagedNotificationsResponseDTO> GetNotificationsPagedAsync(int page, int limit)
    {
        var allNotifications = (await _notificationRepository.GetAllAsync()).OrderByDescending(n => n.SentAt).ToList();
        var totalCount = allNotifications.Count;
        var thisWeekCount = allNotifications.Count(n => n.SentAt > DateTime.UtcNow.AddDays(-7));
        var readCount = allNotifications.Count(n => n.ReadStatus);
        var readRate = totalCount > 0 ? (double)readCount / totalCount : 0.0;
        var pageItems = allNotifications.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedNotificationsResponseDTO
        {
            Notifications = pageItems.Select(MapToResponseDto).ToList(),
            TotalCount = totalCount,
            ThisWeekCount = thisWeekCount,
            ReadRate = readRate,
            Page = page,
            Limit = limit,
            TotalPages = (int)Math.Ceiling((double)totalCount / limit)
        };
    }

    public async Task<int> GetTotalSentCountAsync()
    {
        return await _notificationRepository.CountAllAsync();
    }
    
    public async Task<bool> MarkAsReadAsync(long notificationId, User user)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification == null)
        {
            return false;
        }
        // Only recipient can mark as read; broadcast can be marked by anyone
        if (notification.RecipientId.HasValue && notification.RecipientId.Value != user.Id)
        {
            return false;
        }
        if (!notification.ReadStatus)
        {
            notification.ReadStatus = true;
            await _notificationRepository.UpdateAsync(notification);
        }
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(User user)
    {
        var toUpdate = (await _notificationRepository.GetByRecipientOrderBySentAtDescAsync(user)).Where(n => !n.ReadStatus).ToList();
        var count = 0;
        foreach (var n in toUpdate)
        {
            n.ReadStatus = true;
            await _notificationRepository.UpdateAsync(n);
            count++;
        }
        return count;
    }

    private NotificationMessage CreateNotificationMessage(User sender, string message, string type)
    {
        return new NotificationMessage
        {
            SenderId = sender.Id,
            SenderWorkId = sender.WorkId,
            SenderName = sender.Email,
            Message = message,
            Timestamp = DateTime.Now,
            Type = type
        };
    }
    
    private NotificationResponseDTO MapToResponseDto(Notification notification)
    {
        return new NotificationResponseDTO
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Timestamp = notification.SentAt ?? DateTime.UtcNow,
            SentAt = notification.SentAt ?? DateTime.UtcNow,
            Category = notification.Category.ToString(),
            Priority = notification.Priority.ToString(),
            Status = notification.Status,
            ViaEmail = notification.ViaEmail,
            
            // Sender information
            SenderId = notification.Sender?.Id ?? 0,
            SenderName = notification.Sender?.Email ?? "System",
            SenderRole = notification.Sender?.Role.ToString() ?? "System", // Fixed: Handle nullable Role
            SenderWorkId = notification.Sender?.WorkId ?? "",
            SenderAvatarUrl = null, // You can add this field to User model if needed
            
            // Recipient information
            RecipientId = notification.Recipient?.Id,
            RecipientName = notification.Recipient?.Email ?? "All Users",
            RecipientRole = notification.Recipient?.Role.ToString() ?? null, // Fixed: Handle nullable Role
            RecipientWorkId = notification.Recipient?.WorkId,
            
            // Status information
            Read = notification.ReadStatus, // Use ReadStatus from model
            ReadAt = null, // Fixed: Set to null since property doesn't exist in model
            Archived = false, // Fixed: Set to false since property doesn't exist in model
            ArchivedAt = null, // Fixed: Set to null since property doesn't exist in model
            
            // Context information (you can enhance these based on your business logic)
            ContextType = DetermineContextType(notification),
            ContextId = DetermineContextId(notification),
            ActionRequired = DetermineActionRequired(notification),
            ActionUrl = GenerateActionUrl(notification)
        };
    }
    
    private string DetermineContextType(Notification notification)
    {
        // Fixed: Use existing Category enum values
        return notification.Category switch
        {
            Category.PERFORMANCE => "AGENT",
            Category.ATTENDANCE => "AGENT", 
            Category.TASK => "TASK", // Fixed: Changed from Group to Task
            Category.SYSTEM => "SYSTEM",
            _ => "GENERAL"
        };
    }
    
    private string? DetermineContextId(Notification notification)
    {
        // Logic to extract context ID from notification
        // This would depend on how you structure your notifications
        return notification.Recipient?.Id.ToString();
    }
    
    private string? DetermineActionRequired(Notification notification)
    {
        // Logic to determine if action is required
        return notification.Priority == Priority.URGENT ? "true" : "false";
    }
    
    private string? GenerateActionUrl(Notification notification)
    {
        // Fixed: Use existing Category enum values
        return notification.Category switch
        {
            Category.PERFORMANCE => $"/agent/{notification.Recipient?.Id}/performance",
            Category.ATTENDANCE => $"/agent/{notification.Recipient?.Id}/attendance",
            Category.TASK => $"/task/{DetermineContextId(notification)}", // Fixed: Changed from Group to Task
            _ => null
        };
    }
}