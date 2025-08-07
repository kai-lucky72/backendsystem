using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Notification;

public class PagedNotificationsResponseDTO
{
    public List<NotificationResponseDTO> Notifications { get; set; } = new();
    public int TotalCount { get; set; } // Non-nullable to fix ?? operator errors
    public int ThisWeekCount { get; set; } // Non-nullable to fix ?? operator errors
    public double ReadRate { get; set; } // Non-nullable to fix ?? operator errors
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
}