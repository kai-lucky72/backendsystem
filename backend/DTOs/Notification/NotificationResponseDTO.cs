using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Notification;

public class NotificationResponseDTO
{
    public long Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool? Read { get; set; }
    
    // Enhanced sender information
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string SenderWorkId { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    
    // Enhanced recipient information
    public long? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientRole { get; set; }
    public string? RecipientWorkId { get; set; }
    
    // Category and context information
    public string Category { get; set; } = string.Empty;  // e.g., "PERFORMANCE", "ATTENDANCE", "SYSTEM", "GROUP"
    public string Priority { get; set; } = string.Empty;  // e.g., "LOW", "MEDIUM", "HIGH", "URGENT"
    public string? ContextType { get; set; }  // e.g., "AGENT", "GROUP", "MANAGER", "SYSTEM"
    public string? ContextId { get; set; }  // ID of the related entity (agent ID, group ID, etc.)
    public string? ActionRequired { get; set; }  // Whether any action is required from recipient
    public string? ActionUrl { get; set; }  // Optional URL for taking action
    
    // Status information
    public DateTime? ReadAt { get; set; }  // When the notification was read (if applicable)
    public bool? Archived { get; set; }
    public DateTime? ArchivedAt { get; set; }  // When the notification was archived (if applicable)
}