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

    public NotificationService(
        ILogger<NotificationService> logger,
        INotificationRepository notificationRepository,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Notification> SendNotificationAsync(User sender, User recipient, string title, string message, bool viaEmail, Notification.Category category, Notification.Priority priority)
    {
        var notification = new Notification
        {
            Sender = sender,
            Recipient = recipient,
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

    public async Task<Notification> SendNotificationToAllAsync(User sender, string title, string message, bool viaEmail, Notification.Category category, Notification.Priority priority)
    {
        var notification = new Notification
        {
            Sender = sender,
            Recipient = null, // null for broadcast
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
        return await _notificationRepository.GetBySenderAsync(sender);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsByRecipientAsync(User recipient)
    {
        return await _notificationRepository.GetByRecipientAsync(recipient);
    }

    public async Task<IEnumerable<Notification>> GetBroadcastNotificationsAsync()
    {
        return await _notificationRepository.GetBroadcastNotificationsAsync();
    }
    
    public async Task SendWebSocketNotificationAsync(User sender, User recipient, string message)
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
    }

    public async Task SendWebSocketNotificationToAllAsync(User sender, string message)
    {
        var notification = CreateNotificationMessage(sender, message, "BROADCAST");
        
        // In a real implementation, you would use SignalR or similar WebSocket framework
        // For now, we'll just log the notification
        _logger.LogInformation("WebSocket broadcast notification sent: {Message}", message);
        
        // Send to all connected users
        // messagingTemplate.convertAndSend("/topic/notifications", notification);
    }

    public async Task<Notification> SendCompleteNotificationAsync(User sender, User recipient, string title, string message, bool viaEmail, Notification.Category category, Notification.Priority priority)
    {
        var notification = await SendNotificationAsync(sender, recipient, title, message, viaEmail, category, priority);
        await SendWebSocketNotificationAsync(sender, recipient, message);
        return notification;
    }

    public async Task<Notification> SendCompleteNotificationToAllAsync(User sender, string title, string message, bool viaEmail, Notification.Category category, Notification.Priority priority)
    {
        var notification = await SendNotificationToAllAsync(sender, title, message, viaEmail, category, priority);
        await SendWebSocketNotificationToAllAsync(sender, message);
        return notification;
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
}