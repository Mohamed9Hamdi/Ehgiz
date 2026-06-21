using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}