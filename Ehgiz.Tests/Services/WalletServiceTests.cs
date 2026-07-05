using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.Wallet;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class WalletServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private WalletService _sut = null!;
    private ApplicationUser _user = null!;

    public async ValueTask InitializeAsync()
    {
        _user = await _db.SeedUserAsync();
        _sut = new WalletService(_db.Uow, _stripe, _notifications, _db.UserManager);
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task GetWalletAsync_CreatesWalletOnFirstAccess()
    {
        var result = await _sut.GetWalletAsync(_user.Id);

        Assert.Equal(0m, result.Balance);
        Assert.Equal(0m, result.HeldBalance);
        Assert.Single(_db.Context.Wallets.Where(w => w.UserId == _user.Id).ToList());
    }

    [Fact]
    public async Task GetWalletAsync_ReturnsTotalOfAvailableAndHeld()
    {
        await _db.SeedWalletAsync(_user.Id, balance: 70m, heldBalance: 30m);

        var result = await _sut.GetWalletAsync(_user.Id);

        Assert.Equal(70m, result.Balance);
        Assert.Equal(30m, result.HeldBalance);
        Assert.Equal(100m, result.TotalBalance);
    }

    // ── InitiateTopUpAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task InitiateTopUpAsync_RejectsNonPositiveAmount()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.InitiateTopUpAsync(_user.Id, new TopUpRequest(0m), "https://return"));
    }

    [Fact]
    public async Task InitiateTopUpAsync_CreatesStripeCustomerOnceAndReturnsClientSecret()
    {
        _stripe.CreateOrGetCustomerAsync(_user.Email!, _user.FullName).Returns("cus_123");
        _stripe.CreateCheckoutSessionAsync(50m, "usd", "cus_123", Arg.Any<string>(), _user.Id, "https://return")
            .Returns("secret_abc");

        var result = await _sut.InitiateTopUpAsync(_user.Id, new TopUpRequest(50m), "https://return");

        Assert.Equal("secret_abc", result.ClientSecret);
        Assert.Equal(50m, result.Amount);

        var updated = await _db.UserManager.FindByIdAsync(_user.Id.ToString());
        Assert.Equal("cus_123", updated!.StripeCustomerId);

        // Second top-up must reuse the stored customer id.
        await _sut.InitiateTopUpAsync(_user.Id, new TopUpRequest(20m), "https://return");
        await _stripe.Received(1).CreateOrGetCustomerAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── CreditWalletFromStripeAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreditWalletFromStripeAsync_AddsBalanceRecordsTransactionAndNotifies()
    {
        await _db.SeedWalletAsync(_user.Id, balance: 10m);

        await _sut.CreditWalletFromStripeAsync("cs_session_1", _user.Id, 40m);

        var wallet = _db.Context.Wallets.Single(w => w.UserId == _user.Id);
        Assert.Equal(50m, wallet.Balance);

        var tx = Assert.Single(_db.Context.WalletTransactions.Where(t => t.WalletId == wallet.Id).ToList());
        Assert.Equal(40m, tx.Amount);
        Assert.Equal(WalletTransactionType.TopUp, tx.Type);
        Assert.Equal("cs_session_1", tx.Reference);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _user.Id && n.Type == NotificationType.Payment));
    }

    [Fact]
    public async Task CreditWalletFromStripeAsync_IgnoresDuplicateWebhookDelivery()
    {
        await _db.SeedWalletAsync(_user.Id, balance: 10m);

        await _sut.CreditWalletFromStripeAsync("cs_session_dup", _user.Id, 40m);
        await _sut.CreditWalletFromStripeAsync("cs_session_dup", _user.Id, 40m);

        var wallet = _db.Context.Wallets.Single(w => w.UserId == _user.Id);
        Assert.Equal(50m, wallet.Balance);
        Assert.Single(_db.Context.WalletTransactions.Where(t => t.WalletId == wallet.Id).ToList());
    }

    [Fact]
    public async Task InitiateTopUpAsync_RejectsNonUsdCurrency()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.InitiateTopUpAsync(_user.Id, new TopUpRequest(50m, "eur"), "https://return"));
    }

    // ── GetTransactionHistoryAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetTransactionHistoryAsync_ReturnsOwnTransactionsNewestFirst()
    {
        var wallet = await _db.SeedWalletAsync(_user.Id);
        var other = await _db.SeedUserAsync();
        var otherWallet = await _db.SeedWalletAsync(other.Id);

        _db.Context.WalletTransactions.AddRange(
            new WalletTransaction { WalletId = wallet.Id, Amount = 10m, Type = WalletTransactionType.TopUp, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new WalletTransaction { WalletId = wallet.Id, Amount = -5m, Type = WalletTransactionType.BookingDebit, CreatedAt = DateTime.UtcNow },
            new WalletTransaction { WalletId = otherWallet.Id, Amount = 99m, Type = WalletTransactionType.TopUp, CreatedAt = DateTime.UtcNow });
        await _db.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = (await _sut.GetTransactionHistoryAsync(_user.Id)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(-5m, result[0].Amount);
        Assert.Equal("BookingDebit", result[0].Type);
        Assert.Equal(10m, result[1].Amount);
    }

    // ── GetEarningsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetEarningsAsync_ReturnsOneEntryPerMonthWithGrossNetAndFees()
    {
        var wallet = await _db.SeedWalletAsync(_user.Id);
        var category = await _db.SeedCategoryAsync();
        var tool = await _db.SeedToolAsync(_user.Id, category.Id);
        var renter = await _db.SeedUserAsync();
        var booking = await _db.SeedBookingAsync(tool.Id, renter.Id, BookingStatus.Completed);

        _db.Context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = 90m,
            Type = WalletTransactionType.EarningCredit,
            CreatedAt = DateTime.UtcNow
        });
        _db.Context.PlatformRevenueLedgers.Add(new PlatformRevenueLedger
        {
            BookingId = booking.Id,
            Amount = 10m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.GetEarningsAsync(_user.Id, 3);

        Assert.Equal(3, result.Count);
        var current = result[^1];
        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM"), current.Month);
        Assert.Equal(90m, current.Net);
        Assert.Equal(10m, current.Fees);
        Assert.Equal(100m, current.Gross);
        Assert.All(result.Take(2), m => Assert.Equal(0m, m.Gross));
    }

    [Fact]
    public async Task GetEarningsAsync_ClampsMonthsToValidRange()
    {
        var result = await _sut.GetEarningsAsync(_user.Id, 0);

        Assert.Single(result);
    }

    // ── WithdrawAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task WithdrawAsync_RejectsNonPositiveAmount()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.WithdrawAsync(_user.Id, new WithdrawalRequest(-1m)));
    }

    [Fact]
    public async Task WithdrawAsync_RequiresStripeConnectOnboarding()
    {
        await _db.SeedWalletAsync(_user.Id, balance: 100m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.WithdrawAsync(_user.Id, new WithdrawalRequest(50m)));
        Assert.Contains("onboarding", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WithdrawAsync_RejectsWhenBalanceInsufficient()
    {
        _user.StripeAccountId = "acct_1";
        await _db.UserManager.UpdateAsync(_user);
        await _db.SeedWalletAsync(_user.Id, balance: 10m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.WithdrawAsync(_user.Id, new WithdrawalRequest(50m)));

        await _stripe.DidNotReceiveWithAnyArgs()
            .TransferToConnectAccountAsync(default!, default, default!, default!);
    }

    [Fact]
    public async Task WithdrawAsync_TransfersDeductsAndRecordsTransaction()
    {
        _user.StripeAccountId = "acct_1";
        await _db.UserManager.UpdateAsync(_user);
        await _db.SeedWalletAsync(_user.Id, balance: 100m);

        await _sut.WithdrawAsync(_user.Id, new WithdrawalRequest(60m));

        await _stripe.Received(1).TransferToConnectAccountAsync("acct_1", 60m, "usd", Arg.Any<string>());

        // The debit runs as an atomic UPDATE that bypasses the change tracker,
        // so drop cached instances before re-reading the wallet.
        _db.Context.ChangeTracker.Clear();
        var wallet = _db.Context.Wallets.Single(w => w.UserId == _user.Id);
        Assert.Equal(40m, wallet.Balance);

        var tx = Assert.Single(_db.Context.WalletTransactions.Where(t => t.WalletId == wallet.Id).ToList());
        Assert.Equal(-60m, tx.Amount);
        Assert.Equal(WalletTransactionType.Withdrawal, tx.Type);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _user.Id && n.Type == NotificationType.Payment));
    }

    [Fact]
    public async Task WithdrawAsync_RollsBackDebitWhenStripeTransferFails()
    {
        _user.StripeAccountId = "acct_1";
        await _db.UserManager.UpdateAsync(_user);
        await _db.SeedWalletAsync(_user.Id, balance: 100m);

        _stripe.TransferToConnectAccountAsync(default!, default, default!, default!)
            .ReturnsForAnyArgs<Task>(_ => throw new InvalidOperationException("stripe down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.WithdrawAsync(_user.Id, new WithdrawalRequest(60m)));

        _db.Context.ChangeTracker.Clear();
        var wallet = _db.Context.Wallets.Single(w => w.UserId == _user.Id);
        Assert.Equal(100m, wallet.Balance);
        Assert.Empty(_db.Context.WalletTransactions.Where(t => t.WalletId == wallet.Id).ToList());
    }
}
