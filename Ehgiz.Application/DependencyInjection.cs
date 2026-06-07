using Microsoft.Extensions.DependencyInjection;

namespace Ehgiz.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here, e.g.:
        // services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
