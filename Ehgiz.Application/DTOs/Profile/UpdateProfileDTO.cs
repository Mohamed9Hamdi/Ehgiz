namespace Ehgiz.Application.DTOs.Profile;

public record UpdateProfileDTO(
    string? FullName,
    string? PhoneNumber,
    string? Address,
    string? City);
