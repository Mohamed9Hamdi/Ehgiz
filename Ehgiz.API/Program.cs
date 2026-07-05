using System.Threading.RateLimiting;
using Ehgiz.API.Extensions;
using Ehgiz.Application;
using Ehgiz.Application.Common;
using Ehgiz.DAL;
using Microsoft.AspNetCore.RateLimiting;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDalServices();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAiServices();
builder.Services.AddSignalRServices();
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    // Per-IP limit for the password-reset code endpoints; the per-email limit
    // lives in AuthService. Rejections use the same generic message so the
    // response never reveals whether an account exists.
    options.AddPolicy("password-reset", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(15)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Success(null!, "If an account exists, a password reset code was sent."),
            cancellationToken);
    };
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.BufferBody = true);
builder.Services.AddSwaggerWithAuth();

var app = builder.Build();

await app.UseSwaggerInDevelopmentAsync();
app.UseApplicationMiddleware();
app.MapControllers();
app.MapApplicationHubs();

app.Run();
