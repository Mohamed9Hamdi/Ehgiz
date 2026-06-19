namespace Ehgiz.Application.DTOs.Profile;

public record UserProfileDTO(
    int Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? ProfileImageUrl,
    string? Address,
    string? City,
    DateTime CreatedAt,
    bool IsActive,
    IReadOnlyList<string> Roles);
