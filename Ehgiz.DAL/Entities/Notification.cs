using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public NotificationType? Type { get; set; }
    public string? Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
