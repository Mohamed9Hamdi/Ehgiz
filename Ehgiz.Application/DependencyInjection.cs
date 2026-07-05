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
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Microsoft.Extensions.Options;
using ReviewService = Ehgiz.Application.Services.ReviewService;

namespace Ehgiz.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<AiSettings>(configuration.GetSection("AI"));
        services.Configure<FrontendSettings>(configuration.GetSection("Frontend"));


        // Stripe
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));

        // Cloudinary
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));

        services.Configure<GitHubModelsSettings>(configuration.GetSection("GitHubModels"));
        services.AddSingleton(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
            var githubModelsSettings = sp.GetRequiredService<IOptions<GitHubModelsSettings>>().Value;

            // ApiKeyCredential rejects empty keys; use a placeholder so an
            // unconfigured key fails at the AI call site (handled per feature)
            // instead of crashing DI resolution for every dependent service.
            var apiKey = string.IsNullOrWhiteSpace(aiSettings.ApiKey) ? "unconfigured" : aiSettings.ApiKey;

            return new ChatClient(
                model: githubModelsSettings.Model,
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(aiSettings.Endpoint.TrimEnd('/'))
                });
        });
        services.AddScoped<IToolSuggestionService, ToolSuggestionService>();
        services.AddScoped<IToolPhotoSearchService, ToolPhotoSearchService>();
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
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IToolService, ToolService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IToolAssistantService, ToolAssistantAgentService>();
        services.AddScoped<ISavedSearchService, SavedSearchService>();

        return services;
    }
}
