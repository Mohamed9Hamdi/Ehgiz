namespace Ehgiz.Application.DTOs.Payments;

public record PaymentDto(
    int Id,
    int BookingId,
    decimal Amount,
    string? PaymentMethod,
    string? PaymentStatus,
    string? EscrowStatus,
    DateTime? PaidAt,
    string? StripePaymentIntentId);
