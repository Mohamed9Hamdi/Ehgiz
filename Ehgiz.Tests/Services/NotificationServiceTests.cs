using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class NotificationServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly INotificationBroadcaster _broadcaster = Substitute.For<INotificationBroadcaster>();
    private NotificationService _sut = null!;
    private ApplicationUser _user = null!;
    private ApplicationUser _otherUser = null!;

    public async ValueTask InitializeAsync()
    {
        _sut = new NotificationService(_db.Uow, _broadcaster);
        _user = await _db.SeedUserAsync();
        _otherUser = await _db.SeedUserAsync();
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private async Task<Notification> SeedNotificationAsync(int userId, bool isRead = false)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = "Title",
            Message = "Message",
            Type = NotificationType.System,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow
        };
        _db.Context.Notifications.Add(notification);
        await _db.Context.SaveChangesAsync();
        return notification;
    }

    [Fact]
    public async Task CreateAsync_PersistsAndBroadcastsToUser()
    {
        var dto = new CreateNotificationDto
        {
            UserId = _user.Id,
            Title = "Booking Accepted",
            Message = "Your booking was accepted.",
            Type = NotificationType.Booking,
            Url = "/bookings/1"
        };

        var result = await _sut.CreateAsync(dto);

        Assert.Equal(_user.Id, result.UserId);
        Assert.Equal("Booking", result.Type);
        Assert.False(result.IsRead);

        var stored = Assert.Single(_db.Context.Notifications.Where(n => n.UserId == _user.Id).ToList());
        Assert.Equal("Booking Accepted", stored.Title);

        await _broadcaster.Received(1).SendToUserAsync(_user.Id, Arg.Is<NotificationDto>(n => n.Title == "Booking Accepted"));
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyOwnNotificationsNewestFirst()
    {
        var older = await SeedNotificationAsync(_user.Id);
        older.CreatedAt = DateTime.UtcNow.AddHours(-1);
        await _db.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newer = await SeedNotificationAsync(_user.Id);
        await SeedNotificationAsync(_otherUser.Id);

        var result = await _sut.GetByUserIdAsync(_user.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task GetUnreadByUserIdAsync_ExcludesReadNotifications()
    {
        await SeedNotificationAsync(_user.Id, isRead: true);
        var unread = await SeedNotificationAsync(_user.Id, isRead: false);

        var result = await _sut.GetUnreadByUserIdAsync(_user.Id);

        Assert.Equal(unread.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnread()
    {
        await SeedNotificationAsync(_user.Id, isRead: true);
        await SeedNotificationAsync(_user.Id);
        await SeedNotificationAsync(_user.Id);
        await SeedNotificationAsync(_otherUser.Id);

        Assert.Equal(2, await _sut.GetUnreadCountAsync(_user.Id));
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksAndNotifiesReadStateChange()
    {
        var notification = await SeedNotificationAsync(_user.Id);

        await _sut.MarkAsReadAsync(notification.Id, _user.Id);

        Assert.Equal(0, await _sut.GetUnreadCountAsync(_user.Id));
        await _broadcaster.Received(1).NotifyReadStateChangedAsync(_user.Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_ThrowsWhenNotOwner()
    {
        var notification = await SeedNotificationAsync(_user.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MarkAsReadAsync(notification.Id, _otherUser.Id));
    }

    [Fact]
    public async Task MarkAsReadAsync_ThrowsWhenMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.MarkAsReadAsync(9999, _user.Id));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_OnlyAffectsGivenUser()
    {
        await SeedNotificationAsync(_user.Id);
        await SeedNotificationAsync(_user.Id);
        await SeedNotificationAsync(_otherUser.Id);

        await _sut.MarkAllAsReadAsync(_user.Id);

        Assert.Equal(0, await _sut.GetUnreadCountAsync(_user.Id));
        Assert.Equal(1, await _sut.GetUnreadCountAsync(_otherUser.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnNotification()
    {
        var notification = await SeedNotificationAsync(_user.Id);

        await _sut.DeleteAsync(notification.Id, _user.Id);

        Assert.Empty(_db.Context.Notifications.Where(n => n.Id == notification.Id).ToList());
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotOwner()
    {
        var notification = await SeedNotificationAsync(_user.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(notification.Id, _otherUser.Id));
    }
}
