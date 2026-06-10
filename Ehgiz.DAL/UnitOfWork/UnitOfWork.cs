using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Interfaces;
using Ehgiz.DAL.Interfaces.Repositories;
using Ehgiz.DAL.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

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
        IWalletRepository wallets)
    {
        _context = context;
        WalletTransactions = new Repository<WalletTransaction>(context);
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
        Wallets = wallets;
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

    public IWalletRepository Wallets { get; }

    public IRepository<WalletTransaction> WalletTransactions { get; }

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        _context.Database.BeginTransactionAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
