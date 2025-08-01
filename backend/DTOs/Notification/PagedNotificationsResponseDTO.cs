using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Notification;

public class PagedNotificationsResponseDTO
{
    public List<NotificationResponseDTO> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int ThisWeekCount { get; set; }
    public double ReadRate { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
}