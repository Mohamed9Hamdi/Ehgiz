using Ehgiz.Application.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Ehgiz.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
