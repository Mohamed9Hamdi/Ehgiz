using Ehgiz.Application.DTOs.Payments;

namespace Ehgiz.Application.Interfaces;

public interface IPaymentService
{
    Task HandleWebhookAsync(string json, string stripeSignature);
    Task<PaymentDto?> GetPaymentByBookingAsync(int bookingId);
}
