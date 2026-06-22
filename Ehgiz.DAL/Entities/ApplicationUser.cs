using Microsoft.AspNetCore.Identity;

namespace Ehgiz.DAL.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = null!;
    public string? ProfileImageUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeAccountId { get; set; }

    public ICollection<Tool> OwnedTools { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Conversation> ConversationsAsUser1 { get; set; } = [];
    public ICollection<Conversation> ConversationsAsUser2 { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<IssueReport> IssueReports { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserConnection> UserConnections { get; set; } = [];
    public Wallet? Wallet { get; set; }
    public ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; } = [];
}
