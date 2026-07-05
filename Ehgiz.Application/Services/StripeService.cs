using Ehgiz.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Ehgiz.Application.Services;

public class StripeService : IStripeService
{
    public async Task<string> CreateCheckoutSessionAsync(
        decimal amount, string currency, string customerId,
        string description, int userId, string returnUrl)
    {
        var options = new SessionCreateOptions
        {
            UiMode = "embedded_page",
            Mode = "payment",
            Customer = customerId,
            ReturnUrl = returnUrl + "?session_id={CHECKOUT_SESSION_ID}",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = (long)decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = description
                        }
                    },
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "description", description }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.ClientSecret!;
    }

    public async Task<string> CreateOrGetCustomerAsync(string email, string fullName)
    {
        var service = new CustomerService();

        // Check if customer already exists by email
        var listOptions = new CustomerListOptions
        {
            Email = email,
            Limit = 1
        };
        var existing = await service.ListAsync(listOptions);
        if (existing.Data.Count > 0)
            return existing.Data[0].Id;

        // Create new customer
        var createOptions = new CustomerCreateOptions
        {
            Email = email,
            Name = fullName
        };
        var customer = await service.CreateAsync(createOptions);
        return customer.Id;
    }

    public async Task<string> CreateConnectAccountAsync(string email)
    {
        var options = new AccountCreateOptions
        {
            Type = "express",
            Email = email,
            Capabilities = new AccountCapabilitiesOptions
            {
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            }
        };

        var service = new AccountService();
        var account = await service.CreateAsync(options);
        return account.Id;
    }

    public async Task<string> CreateConnectAccountLinkAsync(
        string stripeAccountId, string returnUrl, string refreshUrl)
    {
        var options = new AccountLinkCreateOptions
        {
            Account = stripeAccountId,
            ReturnUrl = returnUrl,
            RefreshUrl = refreshUrl,
            Type = "account_onboarding"
        };

        var service = new AccountLinkService();
        var link = await service.CreateAsync(options);
        return link.Url;
    }

    public async Task TransferToConnectAccountAsync(
        string stripeAccountId, decimal amount, string currency, string description)
    {
        var options = new TransferCreateOptions
        {
            Amount = (long)decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero),
            Currency = currency,
            Destination = stripeAccountId,
            Description = description
        };

        var service = new TransferService();
        await service.CreateAsync(options);
    }

    public async Task RefundCheckoutSessionAsync(string sessionId)
    {
        // Retrieve the Checkout Session to get the underlying PaymentIntent ID
        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(sessionId);

        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = session.PaymentIntentId
        };

        var refundService = new RefundService();
        await refundService.CreateAsync(refundOptions);
    }
}

