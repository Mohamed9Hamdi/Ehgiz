using Ehgiz.Application.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Ehgiz.API.Extensions;

public static class AiExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AiServices");

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                logger.LogWarning(
                    "AI API key is not configured. Set AI__ApiKey in .env. AI assistant endpoints will return 503.");
            }

            // ApiKeyCredential rejects empty keys; use a placeholder so an
            // unconfigured key surfaces as the controller's 503 instead of a
            // DI crash while constructing the client.
            var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey) ? "unconfigured" : settings.ApiKey;

            var openAiClient = new OpenAIClient(
                new System.ClientModel.ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(settings.Endpoint)
                });

            var innerClient = openAiClient.GetChatClient(settings.Model).AsIChatClient();

            return new ChatClientBuilder(innerClient)
                .UseFunctionInvocation()
                .Build(sp);
        });

        return services;
    }
}
