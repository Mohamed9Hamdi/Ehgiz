using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.Wallet;
using Ehgiz.Application.Interfaces;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        return wallet.Adapt<WalletDto>();
    }

    // Wallet balances are denominated in USD; accepting any other currency here
    // would let a checkout paid in a cheap currency be credited 1:1 as dollars.
    private const string WalletCurrency = "egp";
    private const decimal MaxTopUpAmount = 100_000m;

    public async Task<TopUpResponse> InitiateTopUpAsync(int userId, TopUpRequest request, string returnUrl)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Top-up amount must be greater than zero.");

        if (request.Amount > MaxTopUpAmount)
            throw new InvalidOperationException($"Top-up amount cannot exceed {MaxTopUpAmount:C}.");

        if (decimal.Round(request.Amount, 2) != request.Amount)
            throw new InvalidOperationException("Top-up amount cannot have more than two decimal places.");

        if (!string.Equals(request.Currency, WalletCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only EGP top-ups are supported.");

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
            WalletCurrency,
            user.StripeCustomerId,
            description,
            userId,
            returnUrl);

        return new TopUpResponse(
            ClientSecret: clientSecret,
            Amount: request.Amount,
            Currency: WalletCurrency);
    }

    public async Task CreditWalletFromStripeAsync(string sessionId, int userId, decimal amount)
    {
        var wallet = await _uow.Wallets.GetOrCreateByUserIdAsync(userId);

        // Stripe retries webhooks; a redelivered checkout.session.completed
        // event must not credit the wallet a second time.
        var alreadyCredited = await _uow.WalletTransactions.Query()
            .AnyAsync(t => t.WalletId == wallet.Id
                && t.Type == WalletTransactionType.TopUp
                && t.Reference == sessionId);

        if (alreadyCredited)
            return;

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
        return await _uow.WalletTransactions.Query()
            .Where(t => t.Wallet.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ProjectToType<WalletTransactionDto>()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MonthlyEarningsDto>> GetEarningsAsync(int userId, int months)
    {
        months = Math.Clamp(months, 1, 60);

        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(months - 1));

        // Net earnings are what actually hit the owner's wallet; platform fees
        // are recorded per booking in the revenue ledger. Gross = net + fees.
        var net = await _uow.WalletTransactions.Query()
            .Where(t => t.Wallet.UserId == userId
                && t.Type == WalletTransactionType.EarningCredit
                && t.CreatedAt >= start)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        var fees = await _uow.PlatformRevenueLedgers.Query()
            .Where(r => r.Booking.Tool.OwnerId == userId && r.CreatedAt >= start)
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(r => r.Amount) })
            .ToListAsync();

        var result = new List<MonthlyEarningsDto>(months);
        for (var i = 0; i < months; i++)
        {
            var month = start.AddMonths(i);
            var netTotal = net.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Total ?? 0m;
            var feeTotal = fees.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Total ?? 0m;

            result.Add(new MonthlyEarningsDto(
                Month: month.ToString("yyyy-MM"),
                Gross: netTotal + feeTotal,
                Fees: feeTotal,
                Net: netTotal));
        }

        return result;
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

        if (decimal.Round(request.Amount, 2) != request.Amount)
            throw new InvalidOperationException("Withdrawal amount cannot have more than two decimal places.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrEmpty(user.StripeAccountId))
            throw new InvalidOperationException(
                "You must complete Stripe Connect onboarding before withdrawing.");

        var wallet = await _uow.Wallets.GetOrCreateByUserIdAsync(userId);

        if (wallet.Balance < request.Amount)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: {wallet.Balance:C}, Requested: {request.Amount:C}");

        // Debit first with an atomic balance-guarded UPDATE so two concurrent
        // withdrawals cannot both pass the check and overdraw; the Stripe
        // transfer runs inside the transaction so a failed transfer rolls the
        // debit back.
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var debited = await _uow.Wallets.TryDebitBalanceAsync(wallet.Id, request.Amount);
            if (!debited)
                throw new InvalidOperationException(
                    $"Insufficient balance. Requested: {request.Amount:C}");

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

            await _stripe.TransferToConnectAccountAsync(
                user.StripeAccountId,
                request.Amount,
                WalletCurrency,
                $"Withdrawal for user {userId}");
        });

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
