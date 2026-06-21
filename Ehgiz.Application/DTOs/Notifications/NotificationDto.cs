namespace Ehgiz.Application.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
}
