using Ehgiz.Application.DTOs.Messages;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.Application.Services;

public class MessageService : IMessageService
{
    private readonly IUnitOfWork _uow;
    private readonly IMessageBroadcaster _broadcaster;
    private readonly INotificationService _notificationService;

    public MessageService(IUnitOfWork uow, IMessageBroadcaster broadcaster, INotificationService notificationService)
    {
        _uow = uow;
        _broadcaster = broadcaster;
        _notificationService = notificationService;
    }

    public async Task<ConversationDto> GetOrCreateConversationAsync(int currentUserId, int otherUserId)
    {
        if (currentUserId == otherUserId)
            throw new InvalidOperationException("Cannot start a conversation with yourself.");

        var existing = await _uow.Conversations.GetByUsersAsync(currentUserId, otherUserId);
        if (existing is not null)
            return await BuildSingleConversationDtoAsync(existing, currentUserId);

        var otherUser = await _uow.Users.GetByIdAsync(otherUserId)
            ?? throw new KeyNotFoundException($"User {otherUserId} not found.");

        var conversation = new Conversation
        {
            User1Id = currentUserId,
            User2Id = otherUserId,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.Conversations.AddAsync(conversation);
        await _uow.SaveChangesAsync();

        return new ConversationDto
        {
            Id = conversation.Id,
            OtherUserId = otherUser.Id,
            OtherUserName = otherUser.FullName,
            OtherUserAvatarUrl = otherUser.ProfileImageUrl,
            UpdatedAt = conversation.UpdatedAt,
            LastMessage = null,
            UnreadCount = 0
        };
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(int userId)
    {
        var conversations = await _uow.Conversations.GetByUserIdWithDetailsAsync(userId);
        if (conversations.Count == 0)
            return [];

        var ids = conversations.Select(c => c.Id).ToList();
        var unreadMap = await _uow.Messages.GetUnreadCountsByUserAsync(userId, ids);

        return conversations.Select(c => BuildConversationDto(c, userId, unreadMap)).ToList();
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(int conversationId, int userId, int page, int pageSize)
    {
        var conversation = await _uow.Conversations.GetByIdAsync(conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        if (conversation.User1Id != userId && conversation.User2Id != userId)
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");

        return await _uow.Messages.Query()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<MessageDto>()
            .ToListAsync();
    }

    public async Task<MessageDto> SendMessageAsync(int conversationId, int senderId, SendMessageDto dto)
    {
        var conversation = await _uow.Conversations.GetByIdAsync(conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        if (conversation.User1Id != senderId && conversation.User2Id != senderId)
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = dto.Content,
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Messages.AddAsync(message);

        conversation.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();

        var sender = await _uow.Users.GetByIdAsync(senderId)
            ?? throw new KeyNotFoundException($"Sender user {senderId} not found.");
        message.Sender = sender;

        var result = message.Adapt<MessageDto>();

        var recipientId = conversation.User1Id == senderId ? conversation.User2Id : conversation.User1Id;
        await _broadcaster.SendMessageAsync(recipientId, result);
        await _broadcaster.SendMessageAsync(senderId, result);

        var preview = dto.Content.Length > 100 ? dto.Content[..100] + "…" : dto.Content;
        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = recipientId,
            Title = $"{sender.FullName} sent you a message",
            Message = preview,
            Type = NotificationType.Message,
            Url = $"/conversations/{conversationId}"
        });

        return result;
    }

    public async Task MarkAsReadAsync(int conversationId, int userId)
    {
        var conversation = await _uow.Conversations.GetByIdAsync(conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        if (conversation.User1Id != userId && conversation.User2Id != userId)
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");

        await _uow.Messages.MarkConversationAsReadAsync(conversationId, userId);

        var senderId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
        await _broadcaster.NotifyReadAsync(senderId, conversationId);
    }

    private static ConversationDto BuildConversationDto(
        Conversation c, int currentUserId, Dictionary<int, int> unreadMap)
    {
        var otherUser = c.User1Id == currentUserId ? c.User2 : c.User1;
        var lastMsg = c.Messages.Count > 0 ? c.Messages.First().Adapt<MessageDto>() : null;

        return new ConversationDto
        {
            Id = c.Id,
            OtherUserId = otherUser.Id,
            OtherUserName = otherUser.FullName,
            OtherUserAvatarUrl = otherUser.ProfileImageUrl,
            UpdatedAt = c.UpdatedAt,
            LastMessage = lastMsg,
            UnreadCount = unreadMap.GetValueOrDefault(c.Id, 0)
        };
    }

    private async Task<ConversationDto> BuildSingleConversationDtoAsync(Conversation c, int currentUserId)
    {
        int otherUserId = c.User1Id == currentUserId ? c.User2Id : c.User1Id;
        var otherUser = await _uow.Users.GetByIdAsync(otherUserId)
            ?? throw new KeyNotFoundException($"User {otherUserId} not found.");

        return new ConversationDto
        {
            Id = c.Id,
            OtherUserId = otherUser.Id,
            OtherUserName = otherUser.FullName,
            OtherUserAvatarUrl = otherUser.ProfileImageUrl,
            UpdatedAt = c.UpdatedAt,
            LastMessage = null,
            UnreadCount = 0
        };
    }
}
