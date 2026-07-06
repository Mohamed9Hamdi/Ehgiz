namespace Ehgiz.Application.DTOs.Categories;

/// <summary>Public-facing category, used by the browse filters and add/edit-tool dropdowns.</summary>
public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int ToolCount);
