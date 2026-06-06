using Ehgiz.DAL.Enums;

namespace Ehgiz.DAL.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SenderId { get; set; } = null!;
    public string? Content { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
