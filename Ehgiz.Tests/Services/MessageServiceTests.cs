using Ehgiz.Application.DTOs.Messages;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class MessageServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly IMessageBroadcaster _broadcaster = Substitute.For<IMessageBroadcaster>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private MessageService _sut = null!;
    private ApplicationUser _alice = null!;
    private ApplicationUser _bob = null!;

    public async ValueTask InitializeAsync()
    {
        _sut = new MessageService(_db.Uow, _broadcaster, _notifications);
        _alice = await _db.SeedUserAsync(fullName: "Alice");
        _bob = await _db.SeedUserAsync(fullName: "Bob");
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task GetOrCreateConversation_RejectsSelfConversation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetOrCreateConversationAsync(_alice.Id, _alice.Id));
    }

    [Fact]
    public async Task GetOrCreateConversation_ThrowsWhenOtherUserMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetOrCreateConversationAsync(_alice.Id, 999));
    }

    [Fact]
    public async Task GetOrCreateConversation_ReusesExistingConversationInEitherDirection()
    {
        var first = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        var second = await _sut.GetOrCreateConversationAsync(_bob.Id, _alice.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_db.Context.Conversations.ToList());
        Assert.Equal("Alice", second.OtherUserName);
    }

    [Fact]
    public async Task SendMessage_RejectsNonParticipant()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        var eve = await _db.SeedUserAsync(fullName: "Eve");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.SendMessageAsync(conversation.Id, eve.Id, new SendMessageDto { Content = "hi" }));
    }

    [Fact]
    public async Task SendMessage_PersistsBroadcastsToBothAndNotifiesRecipient()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);

        var result = await _sut.SendMessageAsync(conversation.Id, _alice.Id, new SendMessageDto { Content = "hello bob" });

        Assert.Equal("hello bob", result.Content);
        Assert.Equal("Alice", result.SenderName);
        Assert.Equal(nameof(MessageStatus.Sent), result.Status);

        await _broadcaster.Received(1).SendMessageAsync(_bob.Id, Arg.Any<MessageDto>());
        await _broadcaster.Received(1).SendMessageAsync(_alice.Id, Arg.Any<MessageDto>());
        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _bob.Id && n.Type == NotificationType.Message));
    }

    [Fact]
    public async Task SendMessage_TruncatesLongNotificationPreview()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        var longText = new string('x', 150);

        await _sut.SendMessageAsync(conversation.Id, _alice.Id, new SendMessageDto { Content = longText });

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.Message.Length == 101 && n.Message.EndsWith("…")));
    }

    [Fact]
    public async Task GetMessages_PaginatesNewestFirst()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        for (var i = 1; i <= 5; i++)
        {
            _db.Context.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderId = _alice.Id,
                Content = $"msg {i}",
                Status = MessageStatus.Sent,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await _db.Context.SaveChangesAsync();

        var page1 = await _sut.GetMessagesAsync(conversation.Id, _bob.Id, page: 1, pageSize: 2);

        Assert.Equal(2, page1.Count);
        Assert.Equal("msg 5", page1[0].Content);
        Assert.Equal("msg 4", page1[1].Content);
    }

    [Fact]
    public async Task GetMessages_RejectsNonParticipant()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        var eve = await _db.SeedUserAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetMessagesAsync(conversation.Id, eve.Id, 1, 10));
    }

    [Fact]
    public async Task MarkAsRead_MarksIncomingMessagesAndNotifiesSender()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        await _sut.SendMessageAsync(conversation.Id, _alice.Id, new SendMessageDto { Content = "unread" });

        await _sut.MarkAsReadAsync(conversation.Id, _bob.Id);

        // ExecuteUpdate writes directly to the database, so bypass the change tracker.
        var message = _db.Context.Messages.AsNoTracking()
            .Single(m => m.ConversationId == conversation.Id);
        Assert.Equal(MessageStatus.Read, message.Status);
        Assert.NotNull(message.ReadAt);
        await _broadcaster.Received(1).NotifyReadAsync(_alice.Id, conversation.Id);
    }

    [Fact]
    public async Task GetConversations_ReturnsUnreadCountsAndLastMessage()
    {
        var conversation = await _sut.GetOrCreateConversationAsync(_alice.Id, _bob.Id);
        await _sut.SendMessageAsync(conversation.Id, _alice.Id, new SendMessageDto { Content = "one" });
        await _sut.SendMessageAsync(conversation.Id, _alice.Id, new SendMessageDto { Content = "two" });

        var bobView = await _sut.GetConversationsAsync(_bob.Id);

        var dto = Assert.Single(bobView);
        Assert.Equal(2, dto.UnreadCount);
        Assert.Equal("two", dto.LastMessage!.Content);
        Assert.Equal("Alice", dto.OtherUserName);
    }
}
