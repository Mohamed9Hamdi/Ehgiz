using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string? Content { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}
