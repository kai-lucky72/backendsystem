using backend.DTOs.Notification;
using backend.Models;

namespace backend.Services;

public interface INotificationService
{
    // Regular database notifications with optional email
    Task<Notification> SendNotificationAsync(User sender, User recipient, string title, string message, bool viaEmail, Category category, Priority priority);
    
    Task<Notification> SendNotificationToAllAsync(User sender, string title, string message, bool viaEmail, Category category, Priority priority);
    
    Task<IEnumerable<Notification>> GetNotificationsBySenderAsync(User sender);
    
    Task<IEnumerable<Notification>> GetNotificationsByRecipientAsync(User recipient);
    
    Task<IEnumerable<Notification>> GetBroadcastNotificationsAsync();
    
    // WebSocket specific notifications (real-time)
    Task SendWebSocketNotificationAsync(User sender, User recipient, string message);
    
    Task SendWebSocketNotificationToAllAsync(User sender, string message);
    
    // Combined approach - database, email and websocket
    Task<Notification> SendCompleteNotificationAsync(User sender, User recipient, string title, string message, bool viaEmail, Category category, Priority priority);
    
    Task<Notification> SendCompleteNotificationToAllAsync(User sender, string title, string message, bool viaEmail, Category category, Priority priority);

    // Additional methods for AdminController
    Task<Notification> SendNotificationAsync(Dictionary<string, string> body, User sender);
    
    Task<PagedNotificationsResponseDTO> GetNotificationsPagedAsync(int page, int limit);
    
    Task<int> GetTotalSentCountAsync();

    // Read status updates
    Task<bool> MarkAsReadAsync(long notificationId, User user);
    Task<int> MarkAllAsReadAsync(User user);
}