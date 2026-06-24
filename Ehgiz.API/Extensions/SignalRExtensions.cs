using Ehgiz.API.Infrastructure;
using Ehgiz.Application.Interfaces;

namespace Ehgiz.API.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<INotificationBroadcaster, NotificationBroadcaster>();
        services.AddScoped<IMessageBroadcaster, MessageBroadcaster>();

        return services;
    }
}
