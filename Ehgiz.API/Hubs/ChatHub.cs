using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Ehgiz.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _uow;

    public ChatHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(GetUserId()));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(GetUserId()));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartTyping(int conversationId)
    {
        var userId = GetUserId();
        var conversation = await _uow.Conversations.GetByIdAsync(conversationId);
        if (conversation is null) return;
        if (conversation.User1Id != userId && conversation.User2Id != userId) return;

        var recipientId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
        await Clients.Group(UserGroup(recipientId))
            .SendAsync("UserTyping", new { conversationId, userId, isTyping = true });
    }

    public async Task StopTyping(int conversationId)
    {
        var userId = GetUserId();
        var conversation = await _uow.Conversations.GetByIdAsync(conversationId);
        if (conversation is null) return;
        if (conversation.User1Id != userId && conversation.User2Id != userId) return;

        var recipientId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
        await Clients.Group(UserGroup(recipientId))
            .SendAsync("UserTyping", new { conversationId, userId, isTyping = false });
    }

    private int GetUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : throw new HubException("Unauthorized");
    }

    private static string UserGroup(int userId) => $"chat_user_{userId}";
}
