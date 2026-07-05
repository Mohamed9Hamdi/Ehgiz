using System.Threading.RateLimiting;
using Ehgiz.API.Extensions;
using Ehgiz.Application;
using Ehgiz.Application.Common;
using Ehgiz.DAL;
using Ehgiz.DAL.Data;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddDalServices();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAiServices();
builder.Services.AddSignalRServices();
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    // Per-IP limit for the endpoints that send or check one-time email codes
    // (password reset + email verification); the per-email limits live in
    // AuthService. The rejection body is neutral so the response never
    // reveals whether an account exists.
    options.AddPolicy("auth-codes", httpContext =>
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
            ApiResponse<object>.Fail("Too many requests. Please try again later."),
            cancellationToken);
    };
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.BufferBody = true);
builder.Services.AddSwaggerWithAuth();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EhgizDbContext>("database");

var app = builder.Build();

await app.UseSwaggerInDevelopmentAsync();
app.UseApplicationMiddleware();
app.MapControllers();
app.MapApplicationHubs();
app.MapHealthChecks("/health");

app.Run();
