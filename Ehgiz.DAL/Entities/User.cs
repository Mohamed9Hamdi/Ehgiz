namespace Ehgiz.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Tool> OwnedTools { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Conversation> ConversationsAsUser1 { get; set; } = [];
    public ICollection<Conversation> ConversationsAsUser2 { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<IssueReport> IssueReports { get; set; } = [];
}
