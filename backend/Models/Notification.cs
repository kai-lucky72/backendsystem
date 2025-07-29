using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("notifications")]
public class Notification
{
    [Key]
    public long Id { get; set; }

    [Required]
    [ForeignKey("Sender")]
    [Column("sender_id")]
    public long SenderId { get; set; }
    
    [ForeignKey("SenderId")]
    public virtual User Sender { get; set; } = null!;

    [ForeignKey("Recipient")]
    [Column("recipient_id")]
    public long? RecipientId { get; set; }
    
    [ForeignKey("RecipientId")]
    public virtual User? Recipient { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "sent";

    [Required]
    [Column("read_by")]
    public int ReadBy { get; set; } = 0;

    [Required]
    [Column("total_recipients")]
    public int TotalRecipients { get; set; } = 0;

    [Required]
    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("via_email")]
    public bool ViaEmail { get; set; } = false;
    
    [Required]
    [Column("read_status")]
    public bool ReadStatus { get; set; } = false;
    
    [Required]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
    
    [Required]
    public NotificationCategory Category { get; set; } = NotificationCategory.System;
    
    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }
    
    public enum NotificationCategory
    {
        System,
        Attendance,
        Performance,
        Task,
        Other
    }

    // Utility method to get priority string
    public string GetPriorityString()
    {
        return Priority.ToString().ToLower();
    }
}