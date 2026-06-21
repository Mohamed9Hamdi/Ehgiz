using Ehgiz.DAL.Enums;

namespace Ehgiz.Application.DTOs.Notifications;

public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string? Url { get; set; }
}
