namespace Ehgiz.Application.Interfaces;

public interface IStripeService
{
    /// <summary>Creates a PaymentIntent for wallet top-up. Returns the client secret.</summary>
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency,
        string customerId, string description);

    /// <summary>Gets or creates a Stripe Customer for the user.</summary>
    Task<string> CreateOrGetCustomerAsync(string email, string fullName);

    /// <summary>Creates a new Stripe Connect Express account for a tool owner.</summary>
    Task<string> CreateConnectAccountAsync(string email);

    /// <summary>Generates an account onboarding link for Stripe Connect.</summary>
    Task<string> CreateConnectAccountLinkAsync(string stripeAccountId,
        string returnUrl, string refreshUrl);

    /// <summary>Transfers funds from the platform account to a connected owner account.</summary>
    Task TransferToConnectAccountAsync(string stripeAccountId, decimal amount,
        string currency, string description);

    /// <summary>Fully refunds a PaymentIntent.</summary>
    Task RefundPaymentIntentAsync(string paymentIntentId);
}
