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
    string OwnerName,
    string RenterName,
    DateTime StartDate,
    DateTime EndDate,
    int Days,
    decimal RentalCost,
    decimal InsurancePrice,
    decimal TotalPrice,
    string Status,
    string? PaymentStatus,
    string? EscrowStatus,
    DateTime CreatedAt);
