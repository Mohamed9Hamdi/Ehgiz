using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(n => n.Content)
            .HasColumnType("text");

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new Notification
            {
                Id = 1,
                UserId = SeedData.Users.SaraId,
                Type = NotificationType.BookingUpdate,
                Content = "Your booking for Bosch Professional Drill has been completed.",
                IsRead = true,
                CreatedAt = new DateTime(2026, 2, 4, 8, 0, 0, DateTimeKind.Utc)
            },
            new Notification
            {
                Id = 2,
                UserId = SeedData.Users.SaraId,
                Type = NotificationType.PaymentUpdate,
                Content = "Payment of 150 EGP is held in escrow for your ladder booking.",
                IsRead = false,
                CreatedAt = new DateTime(2026, 3, 9, 14, 35, 0, DateTimeKind.Utc)
            },
            new Notification
            {
                Id = 3,
                UserId = SeedData.Users.AhmadId,
                Type = NotificationType.NewMessage,
                Content = "You have a new message from Sara Mohamed.",
                IsRead = false,
                CreatedAt = new DateTime(2026, 3, 8, 11, 0, 0, DateTimeKind.Utc)
            });
    }
}
