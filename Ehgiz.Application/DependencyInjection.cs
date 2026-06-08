using Ehgiz.Application.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Ehgiz.Application.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Ehgiz.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Configure Mapster
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<DatabaseSeeder>();
        // Register Services
       
        
        return services;
    }
}
