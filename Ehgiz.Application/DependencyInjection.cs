using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Ehgiz.Application.Seed;
using Ehgiz.Application.Services;
using Ehgiz.Application.Settings;
using Ehgiz.Application.Interfaces;
using Stripe;

namespace Ehgiz.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));


        // Stripe
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        // Platform settings (fee %)
        services.Configure<PlatformSettings>(configuration.GetSection("Platform"));

        services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));
        services.AddScoped<ITokenService, Ehgiz.Application.Services.TokenService>();
        services.AddScoped<IEmailService, SendGridEmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        // Configure Mapster
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<DatabaseSeeder>();

        // Feature services
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAdminPaymentService, AdminPaymentService>();
        services.AddScoped<IToolService, ToolService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessageService, MessageService>();

        return services;
    }
}
