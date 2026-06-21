using Ehgiz.DAL.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

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
    IWalletRepository Wallets { get; }
    IRepository<Ehgiz.DAL.Entities.WalletTransaction> WalletTransactions { get; }
    IHandoverRepository Handovers { get; }
    IRepository<Ehgiz.DAL.Entities.HandoverImage> HandoverImages { get; }

    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}
