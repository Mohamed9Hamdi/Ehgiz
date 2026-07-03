using System.ClientModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ehgiz.Application.AI;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Ehgiz.Application.Services;

public class ToolSuggestionService : IToolSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ChatClient _chatClient;
    private readonly IUnitOfWork _uow;
    private readonly AiSettings _aiSettings;
    private readonly GitHubModelsSettings _settings;
    private readonly ILogger<ToolSuggestionService> _logger;

    public ToolSuggestionService(
        ChatClient chatClient,
        IUnitOfWork uow,
        IOptions<AiSettings> aiSettings,
        IOptions<GitHubModelsSettings> settings,
        ILogger<ToolSuggestionService> logger)
    {
        _chatClient = chatClient;
        _uow = uow;
        _aiSettings = aiSettings.Value;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ToolSuggestionDto> SuggestFromImagesAsync(
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            throw new InvalidOperationException("AI API key is not configured.");

        ValidateImages(images);

        var categories = (await _uow.Categories.GetAllAsync())
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToList();

        if (categories.Count == 0)
            throw new InvalidOperationException("No active categories are available.");

        var systemPrompt = BuildSystemPrompt(categories);
        var responseContent = await CallGitHubModelsAsync(systemPrompt, images, cancellationToken);
        var parsed = ParseModelResponse(responseContent);

        var category = categories.FirstOrDefault(c => c.Id == parsed.CategoryId)
            ?? throw new InvalidOperationException(
                $"Suggested category {parsed.CategoryId} is not valid. Please try again.");

        if (!Enum.IsDefined(typeof(ToolCondition), parsed.Condition))
            throw new InvalidOperationException("Suggested condition is not valid. Please try again.");

        if (string.IsNullOrWhiteSpace(parsed.Name))
            throw new InvalidOperationException("Could not determine a tool name from the images.");

        if (string.IsNullOrWhiteSpace(parsed.Description) || parsed.Description.Length < 20)
            throw new InvalidOperationException("Could not generate a sufficient description from the images.");

        return new ToolSuggestionDto
        {
            Name = parsed.Name.Trim(),
            Description = parsed.Description.Trim(),
            Condition = parsed.Condition,
            CategoryId = category.Id,
            CategoryName = category.Name
        };
    }

    private void ValidateImages(IReadOnlyList<IFormFile> images)
    {
        AiImageValidator.Validate(images, _settings);
    }

    private static string BuildSystemPrompt(IReadOnlyList<DAL.Entities.Category> categories)
    {
        var template = LoadPromptTemplate();
        var categoryLines = string.Join(Environment.NewLine,
            categories.Select(c => $"- id={c.Id}, name=\"{c.Name}\", description=\"{c.Description ?? string.Empty}\""));

        return template.Replace("{{CATEGORIES}}", categoryLines, StringComparison.Ordinal);
    }

    private static string LoadPromptTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Ehgiz.Application.AI.Prompts.ImageAnalysisPrompt.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Image analysis prompt template was not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private async Task<string> CallGitHubModelsAsync(
        string systemPrompt,
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken)
    {
        var contentParts = new List<ChatMessageContentPart>
        {
            ChatMessageContentPart.CreateTextPart("Analyze these tool photos and return the JSON listing suggestion.")
        };

        foreach (var image in images)
        {
            await using var stream = image.OpenReadStream();
            var bytes = await BinaryData.FromStreamAsync(stream, cancellationToken);
            contentParts.Add(ChatMessageContentPart.CreateImagePart(bytes, image.ContentType));
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(contentParts)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        try
        {
            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = completion.Value.Content[0].Text;

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Image analysis returned an empty response.");

            return content;
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "GitHub Models request failed with status {StatusCode}", ex.Status);
            throw new InvalidOperationException("Image analysis failed. Please try again later.");
        }
    }

    private static ModelSuggestion ParseModelResponse(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<ModelSuggestion>(content, JsonOptions)
                ?? throw new InvalidOperationException("Image analysis returned invalid JSON.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Image analysis returned invalid JSON.", ex);
        }
    }

    private sealed class ModelSuggestion
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("condition")]
        public ToolCondition Condition { get; set; }

        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }
    }
}
