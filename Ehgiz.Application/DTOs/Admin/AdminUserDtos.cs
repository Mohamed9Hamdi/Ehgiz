namespace Ehgiz.Application.DTOs.Admin;

public record AdminUserDto(
    int Id,
    string FullName,
    string Email,
    string? ProfileImageUrl,
    string? City,
    bool IsActive,
    bool EmailConfirmed,
    string Role,
    DateTime CreatedAt);

public record AdminUserDetailsDto(
    int Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? ProfileImageUrl,
    string? Address,
    string? City,
    bool IsActive,
    bool EmailConfirmed,
    string Role,
    DateTime CreatedAt,
    int TotalListings,
    int TotalBookings,
    string? StripeCustomerId,
    string? StripeAccountId);

public record SetUserActiveRequest(bool IsActive);

public record SetUserRoleRequest(string Role);
