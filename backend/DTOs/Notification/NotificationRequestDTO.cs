namespace backend.DTOs.Notification;

public class NotificationRequestDTO

    {
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? SenderRole { get; set; }
    public string? SenderWorkId { get; set; }
}
