using Ehgiz.API.Hubs;
using Ehgiz.API.Middleware;
using Ehgiz.Application.Seed;
using Serilog;

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
        // One structured log line per request (method, path, status, timing).
        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // Baseline security headers for an API: no MIME sniffing, no framing,
        // no referrer leakage to third parties.
        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });

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
