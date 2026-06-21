using Ehgiz.Application.DTOs.Handovers;

namespace Ehgiz.Application.DTOs.Bookings;

public record CreateBookingRequest(
    int ToolId,
    DateTime StartDate,
    DateTime EndDate);

public record CreateBookingResponse(
    int BookingId,
    decimal RentalCost,
    decimal InsuranceAmount,
    decimal PlatformFee,
    decimal TotalCharged,
    string Currency);

public record BookingDto(
    int Id,
    int ToolId,
    string ToolName,
    string? ToolImageUrl,
    int OwnerId,
    string OwnerName,
    string? OwnerProfileImageUrl,
    int RenterId,
    string RenterName,
    string? RenterProfileImageUrl,
    DateTime StartDate,
    DateTime EndDate,
    int Days,
    decimal RentalCost,
    decimal InsurancePrice,
    decimal TotalPrice,
    string Status,
    string? PaymentStatus,
    string? EscrowStatus,
    DateTime CreatedAt,
    string? AdminResolutionNotes,
    IEnumerable<HandoverDto>? Handovers,
    IReadOnlyList<string> AllowedActions,
    bool HasReview);

public record ReportIssueRequest(
    string Title,
    string Description);

public record ToolAvailabilityDto(
    int ToolId,
    int Year,
    int Month,
    IReadOnlyList<BookedDateRange> BookedRanges);

public record BookedDateRange(
    int BookingId,
    DateTime StartDate,
    DateTime EndDate,
    string Status);

