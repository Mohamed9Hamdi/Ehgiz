using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Ehgiz.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IUnitOfWork _uow;

    public NotificationHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        await _uow.UserConnections.AddAsync(new UserConnection
        {
            UserId = userId,
            ConnectionId = Context.ConnectionId,
            ConnectedAt = DateTime.UtcNow,
            IsOnline = true
        });

        await _uow.SaveChangesAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _uow.UserConnections.RemoveByConnectionIdAsync(Context.ConnectionId);

        var userId = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId));

        await base.OnDisconnectedAsync(exception);
    }

    private int GetUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : throw new HubException("Unauthorized");
    }

    private static string GroupName(int userId) => $"user_{userId}";
}
