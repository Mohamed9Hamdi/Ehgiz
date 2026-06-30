using Ehgiz.DAL.Entities;

namespace Ehgiz.DAL.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
}
