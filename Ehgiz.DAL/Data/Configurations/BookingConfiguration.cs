using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.HasOne(b => b.Tool)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.ToolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Renter)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.RenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Booking
            {
                Id = 1,
                ToolId = 1,
                RenterId = SeedData.Users.SaraId,
                StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                TotalPrice = 225m,
                Status = BookingStatus.Completed,
                CreatedAt = SeedData.SeedDate
            },
            new Booking
            {
                Id = 2,
                ToolId = 3,
                RenterId = SeedData.Users.SaraId,
                StartDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                TotalPrice = 150m,
                Status = BookingStatus.Active,
                CreatedAt = SeedData.SeedDate
            });
    }
}
