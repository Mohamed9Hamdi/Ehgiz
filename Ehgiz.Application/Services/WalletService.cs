using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.Wallet;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Ehgiz.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripe;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public WalletService(
        IUnitOfWork uow,
        IStripeService stripe,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _stripe = stripe;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<WalletDto> GetWalletAsync(int userId)
    {
        var wallet = await _uow.Wallets.GetOrCreateByUserIdAsync(userId);
        return new WalletDto(
            Id: wallet.Id,
            Balance: wallet.Balance,
            HeldBalance: wallet.HeldBalance,
            TotalBalance: wallet.Balance + wallet.HeldBalance);
    }

    public async Task<TopUpResponse> InitiateTopUpAsync(int userId, TopUpRequest request, string returnUrl)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Top-up amount must be greater than zero.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        // Get or create Stripe Customer
        if (string.IsNullOrEmpty(user.StripeCustomerId))
        {
            user.StripeCustomerId = await _stripe.CreateOrGetCustomerAsync(user.Email!, user.FullName);
            await _userManager.UpdateAsync(user);
        }

        var description = $"Wallet top-up for user {userId}";
        var clientSecret = await _stripe.CreateCheckoutSessionAsync(
            request.Amount,
            request.Currency,
            user.StripeCustomerId,
            description,
            userId,
            returnUrl);

        return new TopUpResponse(
            ClientSecret: clientSecret,
            Amount: request.Amount,
            Currency: request.Currency);
    }

    public async Task CreditWalletFromStripeAsync(string sessionId, int userId, decimal amount)
    {
        var wallet = await _uow.Wallets.GetOrCreateByUserIdAsync(userId);

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = WalletTransactionType.TopUp,
            Reference = sessionId,
            Description = $"Wallet top-up via Stripe Checkout (Session: {sessionId})",
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = userId,
            Title = "Wallet Topped Up",
            Message = $"Your wallet has been credited with {amount:C}. New balance available for bookings.",
            Type = NotificationType.Payment,
            Url = "/wallet"
        });
    }

    public async Task<IEnumerable<WalletTransactionDto>> GetTransactionHistoryAsync(int userId)
    {
        var wallet = await _uow.Wallets.GetByUserIdWithTransactionsAsync(userId);
        if (wallet is null) return [];

        return wallet.Transactions.Select(t => new WalletTransactionDto(
            Id: t.Id,
            Amount: t.Amount,
            Type: t.Type.ToString(),
            Description: t.Description,
            Reference: t.Reference,
            CreatedAt: t.CreatedAt));
    }

    public async Task<ConnectOnboardingResponse> GetConnectOnboardingUrlAsync(
        int userId, string returnUrl, string refreshUrl)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        // Create Connect account if first time
        if (string.IsNullOrEmpty(user.StripeAccountId))
        {
            user.StripeAccountId = await _stripe.CreateConnectAccountAsync(user.Email!);
            await _userManager.UpdateAsync(user);
        }

        var url = await _stripe.CreateConnectAccountLinkAsync(
            user.StripeAccountId, returnUrl, refreshUrl);

        return new ConnectOnboardingResponse(OnboardingUrl: url);
    }

    public async Task WithdrawAsync(int userId, WithdrawalRequest request)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Withdrawal amount must be greater than zero.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrEmpty(user.StripeAccountId))
            throw new InvalidOperationException(
                "You must complete Stripe Connect onboarding before withdrawing.");

        var wallet = await _uow.Wallets.GetOrCreateByUserIdAsync(userId);

        if (wallet.Balance < request.Amount)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: {wallet.Balance:C}, Requested: {request.Amount:C}");

        // Transfer via Stripe
        await _stripe.TransferToConnectAccountAsync(
            user.StripeAccountId,
            request.Amount,
            "usd",
            $"Withdrawal for user {userId}");

        wallet.Balance -= request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -request.Amount,
            Type = WalletTransactionType.Withdrawal,
            Reference = user.StripeAccountId,
            Description = $"Withdrawal of {request.Amount:C} to bank account",
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();

        await _notificationService.CreateAsync(new CreateNotificationDto
        {
            UserId = userId,
            Title = "Withdrawal Processed",
            Message = $"Your withdrawal of {request.Amount:C} has been processed successfully.",
            Type = NotificationType.Payment,
            Url = "/wallet"
        });
    }
}
