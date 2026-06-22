namespace Ehgiz.Application.DTOs.Bookings;

/// <summary>
/// Lightweight DTO for booking cards in the "My Bookings" and "Received Bookings" list views.
/// </summary>
public record BookingCardDto(
    int Id,

    // Tool info (minimal for card)
    int ToolId,
    string ToolName,
    string? ToolImageUrl,

    // The other party (owner for renter view, renter for owner view)
    int OtherPartyId,
    string OtherPartyName,
    string? OtherPartyImageUrl,

    // Booking dates & cost
    DateTime StartDate,
    DateTime EndDate,
    int Days,
    decimal TotalPrice,

    // Status
    string Status,
    DateTime CreatedAt,

    // Handover state summaries (no nested images — detail view only)
    HandoverSummaryDto? DeliveryHandover,
    HandoverSummaryDto? ReturnHandover,

    // Server-computed allowed actions for the current user
    IReadOnlyList<string> AllowedActions
);

/// <summary>
/// Minimal handover info for card display — no images.
/// </summary>
public record HandoverSummaryDto(
    int Id,
    bool IsSubmitted,
    bool? IsAccepted,
    DateTime? SubmittedAt,
    DateTime? RespondedAt,
    int ImageCount
);
