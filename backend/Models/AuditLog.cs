using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("audit_log")]
public class AuditLog
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [Column("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [ForeignKey("User")]
    [Column("user_id")]
    public long? UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [Required]
    public DateTime? Timestamp { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }
}