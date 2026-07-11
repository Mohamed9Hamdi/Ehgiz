using System.ComponentModel;
using Ehgiz.Application.DTOs.Ai;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Ehgiz.Application.Services;

public class ToolAssistantAgentService : IToolAssistantService
{
    private readonly IChatClient _chat;
    private readonly IUnitOfWork _uow;
    private readonly List<ToolRecommendationDto> _recommendedTools = [];

    private const string SystemPrompt = """
        You are Ehgiz, a friendly tool rental assistant for a DIY and home repair marketplace.

        Your job:
        1. Understand the user's DIY or repair problem from their message.
        2. ALWAYS call SearchAvailableTools with relevant keywords before recommending any tools.
        3. Only recommend tools that were returned by SearchAvailableTools — never invent listings.
        4. Prefer tools that are available (IsAvailable = true).
        5. Explain why each recommended tool fits the user's problem.
        6. If nothing matches, say so clearly and suggest broader search terms or related categories (use ListCategories if helpful).
        7. Keep answers concise, practical, and helpful.
        """;

    public ToolAssistantAgentService(IChatClient chat, IUnitOfWork uow)
    {
        _chat = chat;
        _uow = uow;
    }

    public async Task<ToolAssistantResponseDto> RunAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _recommendedTools.Clear();

        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(GetType().GetMethod(nameof(SearchAvailableTools))!, this),
                AIFunctionFactory.Create(GetType().GetMethod(nameof(ListCategories))!, this),
                AIFunctionFactory.Create(GetType().GetMethod(nameof(GetToolById))!, this)
            ]
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userMessage)
        };

        var response = await _chat.GetResponseAsync(messages, chatOptions, cancellationToken);

        return new ToolAssistantResponseDto
        {
            Answer = response.Text ?? "I couldn't find a suitable recommendation.",
            RecommendedTools = _recommendedTools
                .DistinctBy(t => t.Id)
                .ToList()
        };
    }

    [Description("Search the available tools catalog by keywords. Pass comma-separated or space-separated search terms (e.g. drill, screw, door). Returns up to 10 matching available tools.")]
    public async Task<IReadOnlyList<ToolSearchResultDto>> SearchAvailableTools(
        [Description("Keywords to search in tool names and descriptions.")] string searchTerms)
    {
        var terms = searchTerms
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count == 0)
            return [];

        var allTools = await _uow.Tools.Query()
            .Where(t => t.IsAvailable && t.Category.IsActive)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                CategoryName = t.Category.Name,
                t.PricePerDay,
                t.Location,
                t.Latitude,
                t.Longitude,
                t.IsAvailable,
                ImageUrls = t.Images.Select(i => i.ImageUrl).ToList(),
                t.CreatedAt
            })
            .ToListAsync();

        // Description is stored as SQL Server `text`, which breaks EF Contains translation.
        // Filter in memory after projecting — fine for v1 catalog size.
        var tools = allTools
            .Where(t => terms.Any(term =>
                t.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)))
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToList();

        foreach (var tool in tools)
        {
            if (_recommendedTools.Any(r => r.Id == tool.Id))
                continue;

            _recommendedTools.Add(new ToolRecommendationDto
            {
                Id = tool.Id,
                Name = tool.Name,
                Description = tool.Description,
                PricePerDay = tool.PricePerDay,
                CategoryName = tool.CategoryName,
                Location = tool.Location,
                Latitude = tool.Latitude,
                Longitude = tool.Longitude,
                ImageUrls = tool.ImageUrls
            });
        }

        return tools
            .Select(t => new ToolSearchResultDto(
                t.Id,
                t.Name,
                t.Description,
                t.CategoryName,
                t.PricePerDay,
                t.Location,
                t.IsAvailable))
            .ToList();
    }

    [Description("List all active tool rental categories to help narrow a search.")]
    public async Task<IReadOnlyList<CategorySearchResultDto>> ListCategories()
    {
        return await _uow.Categories.Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategorySearchResultDto(c.Id, c.Name, c.Description))
            .ToListAsync();
    }

    [Description("Get detailed information about a specific tool by its numeric id.")]
    public async Task<ToolSearchResultDto?> GetToolById(
        [Description("The tool id from search results.")] int id)
    {
        var tool = await _uow.Tools.Query()
            .Where(t => t.Id == id)
            .Select(t => new ToolSearchResultDto(
                t.Id,
                t.Name,
                t.Description,
                t.Category.Name,
                t.PricePerDay,
                t.Location,
                t.IsAvailable))
            .FirstOrDefaultAsync();

        return tool;
    }

    public record CategorySearchResultDto(int Id, string Name, string? Description);
}
