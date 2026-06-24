using Ehgiz.Application.DTOs.Payments;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Settings;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.Extensions.Options;
using Stripe;

namespace Ehgiz.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IWalletService _walletService;
    private readonly StripeSettings _stripe;

    public PaymentService(
        IUnitOfWork uow,
        IWalletService walletService,
        IOptions<StripeSettings> stripe)
    {
        _uow = uow;
        _walletService = walletService;
        _stripe = stripe.Value;
    }

    public async Task HandleWebhookAsync(string json, string stripeSignature)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, stripeSignature, _stripe.WebhookSecret);
        }
        catch (StripeException ex)
        {
            throw new InvalidOperationException($"Stripe webhook validation failed: {ex.Message}");
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session is null) return;

            // Extract userId from metadata
            if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
                !int.TryParse(userIdStr, out var userId))
                return;

            var amount = (session.AmountTotal ?? 0) / 100m;  // convert cents to dollars

            await _walletService.CreditWalletFromStripeAsync(
                session.PaymentIntentId!, userId, amount);
        }
        else if (stripeEvent.Type == "checkout.session.expired")
        {
            // Log / notify — no wallet change needed
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            // Could emit a notification here in the future
        }
    }

    public async Task<PaymentDto?> GetPaymentByBookingAsync(int bookingId)
    {
        var all = await _uow.Payments.GetAllAsync();
        var payment = all.FirstOrDefault(p => p.BookingId == bookingId);
        if (payment is null) return null;

        return new PaymentDto(
            Id: payment.Id,
            BookingId: payment.BookingId,
            Amount: payment.Amount,
            PaymentMethod: payment.PaymentMethod?.ToString(),
            PaymentStatus: payment.PaymentStatus?.ToString(),
            EscrowStatus: payment.EscrowStatus?.ToString(),
            PaidAt: payment.PaidAt,
            StripePaymentIntentId: payment.StripePaymentIntentId);
    }
}
