namespace Ehgiz.Application.DTOs.Admin;

public record AdminCategoryDto(
    int Id,
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    int ToolCount);

public record CreateCategoryRequest(string Name, string? Description, string? ImageUrl);

public record UpdateCategoryRequest(string? Name, string? Description, string? ImageUrl, bool? IsActive);
