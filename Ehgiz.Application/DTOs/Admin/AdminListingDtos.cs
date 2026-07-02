namespace Ehgiz.Application.DTOs.Admin;

public record AdminListingDto(
    int Id,
    string Name,
    string? Description,
    decimal PricePerDay,
    decimal InsurancePrice,
    string? Condition,
    string? Location,
    bool IsAvailable,
    int CategoryId,
    string CategoryName,
    int OwnerId,
    string OwnerName,
    string? FirstImageUrl,
    DateTime CreatedAt);

public record AdminListingDetailsDto(
    int Id,
    string Name,
    string? Description,
    decimal PricePerDay,
    decimal InsurancePrice,
    string? Condition,
    string? Location,
    bool IsAvailable,
    int CategoryId,
    string CategoryName,
    int OwnerId,
    string OwnerName,
    List<string> ImageUrls,
    DateTime CreatedAt,
    int TotalBookings);

public record SetListingAvailabilityRequest(bool IsAvailable);
