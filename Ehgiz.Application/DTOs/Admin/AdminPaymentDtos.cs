namespace Ehgiz.Application.DTOs.Admin;

public record AdminPaymentDto(
    int Id,
    int BookingId,
    string RenterName,
    string OwnerName,
    string ToolName,
    decimal Amount,
    string? PaymentMethod,
    string? PaymentStatus,
    string? EscrowStatus,
    string? StripePaymentIntentId,
    DateTime? PaidAt);
