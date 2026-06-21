namespace Ehgiz.Application.DTOs.Messages;

public class ConversationDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = null!;
    public string? OtherUserAvatarUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}
