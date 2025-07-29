using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Notification;

public class NotificationMessage
{
    public long Id { get; set; }
    public long SenderId { get; set; }
    public string SenderWorkId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty; // Usually email or workId
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // For client-side handling: "DIRECT", "BROADCAST", etc.
}