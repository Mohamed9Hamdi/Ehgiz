using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Ehgiz.DAL.Interfaces.Repositories;
using Ehgiz.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly EhgizDbContext _context;

    public UnitOfWork(
        EhgizDbContext context,
        IUserRepository users,
        ICategoryRepository categories,
        IToolRepository tools,
        IToolImageRepository toolImages,
        IBookingRepository bookings,
        IPaymentRepository payments,
        IReviewRepository reviews,
        IConversationRepository conversations,
        IMessageRepository messages,
        INotificationRepository notifications,
        IIssueReportRepository issueReports,
        IWalletRepository wallets,
        IHandoverRepository handovers,
        IUserConnectionRepository userConnections,
        IRefreshTokenRepository refreshTokens,
        IEmailVerificationCodeRepository emailVerificationCodes,
        IPasswordResetCodeRepository passwordResetCodes)
    {
        _context = context;
        WalletTransactions = new Repository<WalletTransaction>(context);
        HandoverImages = new Repository<HandoverImage>(context);
        PlatformRevenueLedgers = new Repository<PlatformRevenueLedger>(context);
        SystemSettings = new Repository<SystemSetting>(context);
        Users = users;
        Categories = categories;
        Tools = tools;
        ToolImages = toolImages;
        Bookings = bookings;
        Payments = payments;
        Reviews = reviews;
        Conversations = conversations;
        Messages = messages;
        Notifications = notifications;
        IssueReports = issueReports;
        UserConnections = userConnections;
        RefreshTokens = refreshTokens;
        EmailVerificationCodes = emailVerificationCodes;
        PasswordResetCodes = passwordResetCodes;
        Wallets = wallets;
        Handovers = handovers;
    }

    public IUserRepository Users { get; }

    public ICategoryRepository Categories { get; }

    public IToolRepository Tools { get; }

    public IToolImageRepository ToolImages { get; }

    public IBookingRepository Bookings { get; }

    public IPaymentRepository Payments { get; }

    public IReviewRepository Reviews { get; }

    public IConversationRepository Conversations { get; }

    public IMessageRepository Messages { get; }

    public INotificationRepository Notifications { get; }

    public IIssueReportRepository IssueReports { get; }

    public IUserConnectionRepository UserConnections { get; }

    public IRefreshTokenRepository RefreshTokens { get; }

    public IEmailVerificationCodeRepository EmailVerificationCodes { get; }

    public IWalletRepository Wallets { get; }

    public IRepository<WalletTransaction> WalletTransactions { get; }

    public IHandoverRepository Handovers { get; }

    public IRepository<HandoverImage> HandoverImages { get; }

    public IRepository<PlatformRevenueLedger> PlatformRevenueLedgers { get; }

    public IRepository<SystemSetting> SystemSettings { get; }

    public IPasswordResetCodeRepository PasswordResetCodes { get; }

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await operation();
            await transaction.CommitAsync();
        });
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        });
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
