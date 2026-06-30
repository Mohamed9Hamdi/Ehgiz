namespace Ehgiz.Application.DTOs.Ai;

public record ToolSearchResultDto(
    int Id,
    string Name,
    string? Description,
    string CategoryName,
    decimal PricePerDay,
    string? Location,
    bool IsAvailable);
