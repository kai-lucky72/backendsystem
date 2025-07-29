using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class AuditLogDTO
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}