using Ehgiz.Application.Settings;

namespace Ehgiz.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var frontend = configuration.GetSection("Frontend").Get<FrontendSettings>() ?? new FrontendSettings();
        var origins = frontend.AllowedOrigins is { Length: > 0 }
            ? frontend.AllowedOrigins
            : ["http://localhost:4200"];

        services.AddCors(o => o.AddPolicy("Angular", p =>
            p.WithOrigins(origins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials()));

        return services;
    }
}
