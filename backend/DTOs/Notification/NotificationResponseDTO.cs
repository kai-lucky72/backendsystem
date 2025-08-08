using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Notification;

public class NotificationResponseDTO
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime SentAt { get; set; } // Non-nullable to fix the ? operator error
    public bool? Read { get; set; }
    
    // Enhanced sender information
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string SenderWorkId { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    
    // Enhanced recipient information - Fixed naming to match controller usage
    public long? RecipientId { get; set; }
    public string? RecipientName { get; set; } // This is what the controller uses
    public string? RecipientRole { get; set; }
    public string? RecipientWorkId { get; set; }
    
    // Category and context information
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? ContextType { get; set; }
    public string? ContextId { get; set; }
    public string? ActionRequired { get; set; }
    public string? ActionUrl { get; set; }
    
    // Status and metadata information
    public string Status { get; set; } = string.Empty;
    public bool ViaEmail { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool? Archived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}