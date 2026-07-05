using Ehgiz.Application.DTOs.Payments;

namespace Ehgiz.Application.Interfaces;

public interface IPaymentService
{
    Task HandleWebhookAsync(string json, string stripeSignature);

    /// <summary>
    /// Returns the payment for a booking, or null when none exists.
    /// Only the booking's renter or the tool owner may view it.
    /// </summary>
    Task<PaymentDto?> GetPaymentByBookingAsync(int bookingId, int requestingUserId);
}
