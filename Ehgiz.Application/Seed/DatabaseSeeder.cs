using Ehgiz.DAL.Data;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ehgiz.Application.Seed;

public class DatabaseSeeder
{
    private readonly EhgizDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DatabaseSeeder(
        EhgizDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync())
            return;

        var defaultPassword = _configuration["SeedUsers:DefaultPassword"]
            ?? throw new InvalidOperationException("SeedUsers:DefaultPassword is not configured.");

        var ahmad = await CreateUserAsync(
            "ahmad.hassan@ehgiz.com",
            defaultPassword,
            "Ahmad Hassan",
            "+201001234567",
            "https://cdn.ehgiz.com/users/ahmad.jpg",
            "12 Nile Street",
            "Cairo");

        var sara = await CreateUserAsync(
            "sara.mohamed@ehgiz.com",
            defaultPassword,
            "Sara Mohamed",
            "+201076543210",
            "https://cdn.ehgiz.com/users/sara.jpg",
            "45 Garden City",
            "Giza");

        var omar = await CreateUserAsync(
            "omar.ali@ehgiz.com",
            defaultPassword,
            "Omar Ali",
            "+201112223344",
            "https://cdn.ehgiz.com/users/omar.jpg",
            "8 Alexandria Road",
            "Alexandria");

        var drill = new Tool
        {
            OwnerId = ahmad.Id,
            CategoryId = 1,
            Name = "Bosch Professional Drill",
            Description = "18V cordless drill with two batteries and charger.",
            PricePerDay = 75m,
            InsurancePrice = 150m,
            Condition = "Good",
            Location = "Cairo, Maadi",
            IsAvailable = true,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate
        };

        var mower = new Tool
        {
            OwnerId = ahmad.Id,
            CategoryId = 2,
            Name = "Electric Lawn Mower",
            Description = "1600W electric lawn mower suitable for medium gardens.",
            PricePerDay = 90m,
            InsurancePrice = 200m,
            Condition = "Excellent",
            Location = "Cairo, Maadi",
            IsAvailable = true,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate
        };

        var ladder = new Tool
        {
            OwnerId = omar.Id,
            CategoryId = 3,
            Name = "Aluminum Extension Ladder",
            Description = "6-meter extension ladder with safety locks.",
            PricePerDay = 50m,
            InsurancePrice = 100m,
            Condition = "Good",
            Location = "Alexandria, Smouha",
            IsAvailable = false,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate
        };

        var washer = new Tool
        {
            OwnerId = omar.Id,
            CategoryId = 4,
            Name = "Pressure Washer",
            Description = "2000 PSI pressure washer for outdoor cleaning.",
            PricePerDay = 65m,
            InsurancePrice = 120m,
            Condition = "Good",
            Location = "Alexandria, Smouha",
            IsAvailable = true,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate
        };

        _context.Tools.AddRange(drill, mower, ladder, washer);
        await _context.SaveChangesAsync();

        _context.ToolImages.AddRange(
            new ToolImage { ToolId = drill.Id, ImageUrl = "https://cdn.ehgiz.com/tools/drill-1.jpg" },
            new ToolImage { ToolId = drill.Id, ImageUrl = "https://cdn.ehgiz.com/tools/drill-2.jpg" },
            new ToolImage { ToolId = mower.Id, ImageUrl = "https://cdn.ehgiz.com/tools/mower-1.jpg" },
            new ToolImage { ToolId = ladder.Id, ImageUrl = "https://cdn.ehgiz.com/tools/ladder-1.jpg" },
            new ToolImage { ToolId = washer.Id, ImageUrl = "https://cdn.ehgiz.com/tools/washer-1.jpg" });

        var completedBooking = new Booking
        {
            ToolId = drill.Id,
            RenterId = sara.Id,
            StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
            TotalPrice = 225m,
            Status = BookingStatus.Completed,
            CreatedAt = SeedDate
        };

        var activeBooking = new Booking
        {
            ToolId = ladder.Id,
            RenterId = sara.Id,
            StartDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
            TotalPrice = 150m,
            Status = BookingStatus.Active,
            CreatedAt = SeedDate
        };

        _context.Bookings.AddRange(completedBooking, activeBooking);
        await _context.SaveChangesAsync();

        _context.Payments.AddRange(
            new Payment
            {
                BookingId = completedBooking.Id,
                Amount = 225m,
                PaymentMethod = PaymentMethod.CreditCard,
                PaymentStatus = PaymentStatus.Completed,
                EscrowStatus = EscrowStatus.Released,
                PaidAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Payment
            {
                BookingId = activeBooking.Id,
                Amount = 150m,
                PaymentMethod = PaymentMethod.Wallet,
                PaymentStatus = PaymentStatus.Completed,
                EscrowStatus = EscrowStatus.Held,
                PaidAt = new DateTime(2026, 3, 9, 14, 30, 0, DateTimeKind.Utc)
            });

        _context.Reviews.Add(new Review
        {
            BookingId = completedBooking.Id,
            Rating = 5,
            Comment = "Excellent drill, worked perfectly for my home project.",
            CreatedAt = new DateTime(2026, 2, 4, 12, 0, 0, DateTimeKind.Utc)
        });

        var conversation = new Conversation
        {
            User1Id = ahmad.Id,
            User2Id = sara.Id,
            UpdatedAt = new DateTime(2026, 3, 9, 16, 0, 0, DateTimeKind.Utc)
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        _context.Messages.AddRange(
            new Message
            {
                ConversationId = conversation.Id,
                SenderId = sara.Id,
                Content = "Hi Ahmad, is the drill still available next week?",
                Status = MessageStatus.Read,
                CreatedAt = new DateTime(2026, 1, 20, 9, 0, 0, DateTimeKind.Utc)
            },
            new Message
            {
                ConversationId = conversation.Id,
                SenderId = ahmad.Id,
                Content = "Yes, it is available. Let me know your dates.",
                Status = MessageStatus.Read,
                CreatedAt = new DateTime(2026, 1, 20, 9, 15, 0, DateTimeKind.Utc)
            },
            new Message
            {
                ConversationId = conversation.Id,
                SenderId = sara.Id,
                Content = "Can I pick up the ladder on March 10th?",
                Status = MessageStatus.Delivered,
                CreatedAt = new DateTime(2026, 3, 8, 11, 0, 0, DateTimeKind.Utc)
            });

        _context.Notifications.AddRange(
            new Notification
            {
                UserId = sara.Id,
                Type = NotificationType.BookingUpdate,
                Content = "Your booking for Bosch Professional Drill has been completed.",
                IsRead = true,
                CreatedAt = new DateTime(2026, 2, 4, 8, 0, 0, DateTimeKind.Utc)
            },
            new Notification
            {
                UserId = sara.Id,
                Type = NotificationType.PaymentUpdate,
                Content = "Payment of 150 EGP is held in escrow for your ladder booking.",
                IsRead = false,
                CreatedAt = new DateTime(2026, 3, 9, 14, 35, 0, DateTimeKind.Utc)
            },
            new Notification
            {
                UserId = ahmad.Id,
                Type = NotificationType.NewMessage,
                Content = "You have a new message from Sara Mohamed.",
                IsRead = false,
                CreatedAt = new DateTime(2026, 3, 8, 11, 0, 0, DateTimeKind.Utc)
            });

        _context.IssueReports.Add(new IssueReport
        {
            BookingId = activeBooking.Id,
            ReporterId = sara.Id,
            Title = "Ladder lock issue",
            Description = "One of the safety locks on the ladder feels loose.",
            Status = IssueReportStatus.Open,
            CreatedAt = new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc)
        });

        await _context.SaveChangesAsync();
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string email,
        string password,
        string fullName,
        string phoneNumber,
        string profileImageUrl,
        string address,
        string city)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            ProfileImageUrl = profileImageUrl,
            Address = address,
            City = city,
            CreatedAt = SeedDate,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed user '{email}': {errors}");
        }

        return user;
    }
}
