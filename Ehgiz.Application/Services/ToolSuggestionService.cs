using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ehgiz.Application.Services;

public class ToolSuggestionService : IToolSuggestionService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _uow;
    private readonly GitHubModelsSettings _settings;
    private readonly ILogger<ToolSuggestionService> _logger;

    public ToolSuggestionService(
        HttpClient httpClient,
        IUnitOfWork uow,
        IOptions<GitHubModelsSettings> settings,
        ILogger<ToolSuggestionService> logger)
    {
        _httpClient = httpClient;
        _uow = uow;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ToolSuggestionDto> SuggestFromImagesAsync(
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("GitHub Models API key is not configured.");

        ValidateImages(images);

        var categories = (await _uow.Categories.GetAllAsync())
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToList();

        if (categories.Count == 0)
            throw new InvalidOperationException("No active categories are available.");

        var systemPrompt = BuildSystemPrompt(categories);
        var imageParts = await BuildImageContentPartsAsync(images, cancellationToken);
        var responseContent = await CallGitHubModelsAsync(systemPrompt, imageParts, cancellationToken);
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
        if (images is null || images.Count == 0)
            throw new InvalidOperationException("At least one image is required.");

        if (images.Count > _settings.MaxImages)
            throw new InvalidOperationException($"A maximum of {_settings.MaxImages} images is allowed.");

        foreach (var image in images)
        {
            if (image.Length == 0)
                throw new InvalidOperationException("One or more image files are empty.");

            if (image.Length > _settings.MaxImageBytes)
                throw new InvalidOperationException("One or more images exceed the 5 MB size limit.");

            if (string.IsNullOrWhiteSpace(image.ContentType) || !AllowedContentTypes.Contains(image.ContentType))
                throw new InvalidOperationException("Only JPEG, PNG, WEBP, and GIF images are supported.");
        }
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

    private static async Task<List<object>> BuildImageContentPartsAsync(
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken)
    {
        var parts = new List<object>
        {
            new { type = "text", text = "Analyze these tool photos and return the JSON listing suggestion." }
        };

        foreach (var image in images)
        {
            await using var stream = image.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            var base64 = Convert.ToBase64String(memory.ToArray());
            var mimeType = image.ContentType.ToLowerInvariant();

            parts.Add(new
            {
                type = "image_url",
                image_url = new { url = $"data:{mimeType};base64,{base64}" }
            });
        }

        return parts;
    }

    private async Task<string> CallGitHubModelsAsync(
        string systemPrompt,
        List<object> imageParts,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _settings.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = imageParts }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "GitHub Models request failed with status {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                TruncateForLog(responseBody));

            throw new InvalidOperationException("Image analysis failed. Please try again later.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Image analysis returned an empty response.");

        return content;
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

    private static string TruncateForLog(string value, int maxLength = 500)
        => value.Length <= maxLength ? value : value[..maxLength];

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
