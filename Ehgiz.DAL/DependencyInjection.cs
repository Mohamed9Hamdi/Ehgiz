using Ehgiz.DAL.Interfaces;
using Ehgiz.DAL.Interfaces.Repositories;
using Ehgiz.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using UnitOfWorkImplementation = Ehgiz.DAL.UnitOfWork.UnitOfWork;

namespace Ehgiz.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDalServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IToolRepository, ToolRepository>();
        services.AddScoped<IToolImageRepository, ToolImageRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IIssueReportRepository, IssueReportRepository>();
        services.AddScoped<IUserConnectionRepository, UserConnectionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationCodeRepository, EmailVerificationCodeRepository>();
        services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWorkImplementation>();

        return services;
    }
}
