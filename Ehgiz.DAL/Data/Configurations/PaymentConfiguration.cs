using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.EscrowStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(p => p.BookingId)
            .IsUnique();

        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new Payment
            {
                Id = 1,
                BookingId = 1,
                Amount = 225m,
                PaymentMethod = PaymentMethod.CreditCard,
                PaymentStatus = PaymentStatus.Completed,
                EscrowStatus = EscrowStatus.Released,
                PaidAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Payment
            {
                Id = 2,
                BookingId = 2,
                Amount = 150m,
                PaymentMethod = PaymentMethod.Wallet,
                PaymentStatus = PaymentStatus.Completed,
                EscrowStatus = EscrowStatus.Held,
                PaidAt = new DateTime(2026, 3, 9, 14, 30, 0, DateTimeKind.Utc)
            });
    }
}
