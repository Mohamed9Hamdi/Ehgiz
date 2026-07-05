using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.DAL.Interfaces;
using Ehgiz.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace Ehgiz.Tests.TestHelpers;

/// <summary>
/// Spins up the real EhgizDbContext + UnitOfWork on an in-memory SQLite database,
/// so services are exercised against a genuine relational provider instead of mocks.
/// </summary>
public sealed class TestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private ServiceProvider? _identityProvider;

    public EhgizDbContext Context { get; }
    public IUnitOfWork Uow { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EhgizDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new SqliteEhgizDbContext(options);
        Context.Database.EnsureCreated();

        Uow = new DAL.UnitOfWork.UnitOfWork(
            Context,
            new UserRepository(Context),
            new CategoryRepository(Context),
            new ToolRepository(Context),
            new ToolImageRepository(Context),
            new BookingRepository(Context),
            new PaymentRepository(Context),
            new ReviewRepository(Context),
            new ConversationRepository(Context),
            new MessageRepository(Context),
            new NotificationRepository(Context),
            new IssueReportRepository(Context),
            new WalletRepository(Context),
            new HandoverRepository(Context),
            new UserConnectionRepository(Context),
            new RefreshTokenRepository(Context),
            new EmailVerificationCodeRepository(Context),
            new PasswordResetCodeRepository(Context));
    }

    /// <summary>Identity stack (UserManager/SignInManager/RoleManager) wired to the same context,
    /// configured to mirror AuthenticationExtensions.AddIdentityServices.</summary>
    public IServiceProvider IdentityServices => _identityProvider ??= BuildIdentityProvider();

    public UserManager<ApplicationUser> UserManager =>
        IdentityServices.GetRequiredService<UserManager<ApplicationUser>>();

    public SignInManager<ApplicationUser> SignInManager =>
        IdentityServices.GetRequiredService<SignInManager<ApplicationUser>>();

    private ServiceProvider BuildIdentityProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddSingleton(Context);

        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddEntityFrameworkStores<EhgizDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var role in new[] { "user", "admin" })
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                roleManager.CreateAsync(new IdentityRole<int>(role)).GetAwaiter().GetResult();
        }

        return provider;
    }

    // ── Seed helpers ────────────────────────────────────────────────────────

    private int _emailCounter;

    /// <summary>Adds a user directly to the context (no password) — for tests that
    /// don't go through Identity APIs.</summary>
    public async Task<ApplicationUser> SeedUserAsync(
        string? email = null, string fullName = "Test User", bool isActive = true)
    {
        email ??= $"user{Interlocked.Increment(ref _emailCounter)}@test.local";
        var user = new ApplicationUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    /// <summary>Creates a user through UserManager with a real password hash.</summary>
    public async Task<ApplicationUser> CreateIdentityUserAsync(
        string email, string password = "passw0rd1!", bool emailConfirmed = true,
        bool isActive = true, string role = "user")
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            FullName = "Identity User",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        var result = await UserManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await UserManager.AddToRoleAsync(user, role);
        return user;
    }

    public async Task<Category> SeedCategoryAsync(string name = "Power Tools")
    {
        var category = new Category { Name = name, IsActive = true };
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();
        return category;
    }

    public async Task<Tool> SeedToolAsync(
        int ownerId, int categoryId, string name = "Drill",
        decimal pricePerDay = 10m, decimal insurancePrice = 20m,
        string? description = null, string? location = null,
        double? latitude = null, double? longitude = null,
        ToolCondition? condition = null, bool isAvailable = true)
    {
        var tool = new Tool
        {
            OwnerId = ownerId,
            CategoryId = categoryId,
            Name = name,
            Description = description,
            PricePerDay = pricePerDay,
            InsurancePrice = insurancePrice,
            Condition = condition,
            Location = location,
            Latitude = latitude,
            Longitude = longitude,
            IsAvailable = isAvailable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Tools.Add(tool);
        await Context.SaveChangesAsync();
        return tool;
    }

    public async Task<Wallet> SeedWalletAsync(int userId, decimal balance = 0m, decimal heldBalance = 0m)
    {
        var wallet = new Wallet
        {
            UserId = userId,
            Balance = balance,
            HeldBalance = heldBalance,
            UpdatedAt = DateTime.UtcNow
        };
        Context.Wallets.Add(wallet);
        await Context.SaveChangesAsync();
        return wallet;
    }

    public async Task<Booking> SeedBookingAsync(
        int toolId, int renterId, BookingStatus status,
        DateTime? startDate = null, DateTime? endDate = null,
        decimal rentalCost = 30m, decimal insuranceAmount = 20m,
        decimal platformFee = 3m, decimal pricePerDay = 10m,
        string? adminResolutionNotes = null)
    {
        var booking = new Booking
        {
            ToolId = toolId,
            RenterId = renterId,
            StartDate = startDate ?? DateTime.UtcNow.Date.AddDays(1),
            EndDate = endDate ?? DateTime.UtcNow.Date.AddDays(4),
            RentalCost = rentalCost,
            InsuranceAmount = insuranceAmount,
            PlatformFee = platformFee,
            PricePerDay = pricePerDay,
            TotalPrice = rentalCost + insuranceAmount,
            Status = status,
            AdminResolutionNotes = adminResolutionNotes,
            CreatedAt = DateTime.UtcNow
        };
        Context.Bookings.Add(booking);
        await Context.SaveChangesAsync();
        return booking;
    }

    public async ValueTask DisposeAsync()
    {
        if (_identityProvider is not null)
            await _identityProvider.DisposeAsync();
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    /// <summary>SQLite stores decimal as TEXT, which breaks comparisons and aggregations,
    /// so tests persist decimals as REAL instead.</summary>
    private sealed class SqliteEhgizDbContext : EhgizDbContext
    {
        public SqliteEhgizDbContext(DbContextOptions<EhgizDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var decimalToDouble = new ValueConverter<decimal, double>(d => (double)d, d => (decimal)d);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    // Drop SQL Server column types like nvarchar(max)/decimal(18,2);
                    // SQLite infers its own storage types.
                    property.SetColumnType(null);

                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                        property.SetValueConverter(decimalToDouble);
                }
            }
        }
    }
}
