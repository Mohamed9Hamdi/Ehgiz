using System.ClientModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ehgiz.Application.AI;
using Ehgiz.Application.Common;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Ehgiz.Application.Services;

public class ToolPhotoSearchService : IToolPhotoSearchService
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
    private readonly ILogger<ToolPhotoSearchService> _logger;

    public ToolPhotoSearchService(
        ChatClient chatClient,
        IUnitOfWork uow,
        IOptions<AiSettings> aiSettings,
        IOptions<GitHubModelsSettings> settings,
        ILogger<ToolPhotoSearchService> logger)
    {
        _chatClient = chatClient;
        _uow = uow;
        _aiSettings = aiSettings.Value;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PhotoSearchResultDto> SearchByPhotoAsync(
        IReadOnlyList<IFormFile> images,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            throw new InvalidOperationException("AI API key is not configured.");

        AiImageValidator.Validate(images, _settings);

        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 10;

        var systemPrompt = LoadPromptTemplate();
        var responseContent = await CallGitHubModelsAsync(systemPrompt, images, cancellationToken);
        var parsed = ParseModelResponse(responseContent);

        if (string.IsNullOrWhiteSpace(parsed.IdentifiedObject))
            throw new InvalidOperationException("Could not identify an object in the photo.");

        var keywords = BuildSearchKeywords(parsed);
        var matchingTools = await SearchToolsByKeywordsAsync(keywords, page, pageSize);

        return new PhotoSearchResultDto
        {
            IdentifiedObject = parsed.IdentifiedObject.Trim(),
            Brand = string.IsNullOrWhiteSpace(parsed.Brand) ? null : parsed.Brand.Trim(),
            Model = string.IsNullOrWhiteSpace(parsed.Model) ? null : parsed.Model.Trim(),
            SearchKeywords = keywords,
            MatchingTools = matchingTools
        };
    }

    private async Task<PagedResult<ToolDto>> SearchToolsByKeywordsAsync(
        IReadOnlyList<string> keywords,
        int page,
        int pageSize)
    {
        if (keywords.Count == 0)
            return new PagedResult<ToolDto> { Items = [], TotalCount = 0, PageNumber = page, PageSize = pageSize };

        var query = _uow.Tools.Query()
            .Where(t => t.IsAvailable == true)
            .Where(t => keywords.Any(keyword =>
                t.Name.Contains(keyword) ||
                (t.Description != null && t.Description.Contains(keyword))));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<ToolDto>()
            .ToListAsync();

        return new PagedResult<ToolDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    private static IReadOnlyList<string> BuildSearchKeywords(ModelIdentification parsed)
    {
        var terms = new List<string>();

        if (!string.IsNullOrWhiteSpace(parsed.IdentifiedObject))
            terms.Add(parsed.IdentifiedObject.Trim());

        if (!string.IsNullOrWhiteSpace(parsed.Brand))
            terms.Add(parsed.Brand.Trim());

        if (!string.IsNullOrWhiteSpace(parsed.Model))
            terms.Add(parsed.Model.Trim());

        if (parsed.SearchKeywords is not null)
        {
            foreach (var keyword in parsed.SearchKeywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword))
                    terms.Add(keyword.Trim());
            }
        }

        return terms
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string LoadPromptTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Ehgiz.Application.AI.Prompts.PhotoSearchPrompt.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Photo search prompt template was not found.");

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
            ChatMessageContentPart.CreateTextPart("Identify the tool in this photo and return the JSON object.")
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
                throw new InvalidOperationException("Photo search returned an empty response.");

            return content;
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "GitHub Models request failed with status {StatusCode}", ex.Status);
            throw new InvalidOperationException("Photo search failed. Please try again later.");
        }
    }

    private static ModelIdentification ParseModelResponse(string content)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<ModelIdentification>(content, JsonOptions)
                ?? throw new InvalidOperationException("Photo search returned invalid JSON.");

            if (parsed.SearchKeywords is null || parsed.SearchKeywords.Count == 0)
                throw new InvalidOperationException("Photo search did not return search keywords.");

            return parsed;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Photo search returned invalid JSON.", ex);
        }
    }

    private sealed class ModelIdentification
    {
        public string IdentifiedObject { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public List<string> SearchKeywords { get; set; } = [];
    }
}
