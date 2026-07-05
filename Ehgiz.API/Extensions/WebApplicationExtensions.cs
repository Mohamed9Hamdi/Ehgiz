using Ehgiz.API.Hubs;
using Ehgiz.API.Middleware;
using Ehgiz.Application.Seed;

namespace Ehgiz.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task UseSwaggerInDevelopmentAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ehgiz API v1");
        });

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseCors("Angular");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapApplicationHubs(this WebApplication app)
    {
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<ChatHub>("/hubs/chat");

        return app;
    }
}
