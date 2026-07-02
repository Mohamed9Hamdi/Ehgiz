using Ehgiz.DAL.Interfaces.Repositories;

namespace Ehgiz.DAL.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    ICategoryRepository Categories { get; }
    IToolRepository Tools { get; }
    IToolImageRepository ToolImages { get; }
    IBookingRepository Bookings { get; }
    IPaymentRepository Payments { get; }
    IReviewRepository Reviews { get; }
    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }
    INotificationRepository Notifications { get; }
    IIssueReportRepository IssueReports { get; }
    IUserConnectionRepository UserConnections { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IEmailVerificationCodeRepository EmailVerificationCodes { get; }
    IPasswordResetCodeRepository PasswordResetCodes { get; }

    IWalletRepository Wallets { get; }
    IRepository<Ehgiz.DAL.Entities.WalletTransaction> WalletTransactions { get; }
    IHandoverRepository Handovers { get; }
    IRepository<Ehgiz.DAL.Entities.HandoverImage> HandoverImages { get; }
    IRepository<Ehgiz.DAL.Entities.PlatformRevenueLedger> PlatformRevenueLedgers { get; }
    IRepository<Ehgiz.DAL.Entities.SystemSetting> SystemSettings { get; }
    Task<int> SaveChangesAsync();

    // Runs operation inside a transaction via the configured execution strategy, so it composes with EnableRetryOnFailure.
    Task ExecuteInTransactionAsync(Func<Task> operation);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
