using Ehgiz.Application.Interfaces;
using Stripe;

namespace Ehgiz.Application.Services;

public class StripeService : IStripeService
{
    public async Task<string> CreatePaymentIntentAsync(
        decimal amount, string currency, string customerId, string description)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),   // Stripe works in cents
            Currency = currency,
            Customer = customerId,
            Description = description,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                { "description", description }
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);
        return intent.ClientSecret!;
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
            Amount = (long)(amount * 100),
            Currency = currency,
            Destination = stripeAccountId,
            Description = description
        };

        var service = new TransferService();
        await service.CreateAsync(options);
    }

    public async Task RefundPaymentIntentAsync(string paymentIntentId)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId
        };

        var service = new RefundService();
        await service.CreateAsync(options);
    }
}
