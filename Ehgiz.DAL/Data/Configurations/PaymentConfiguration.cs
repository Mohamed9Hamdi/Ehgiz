using Ehgiz.DAL.Entities;
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

        builder.Property(p => p.StripePaymentIntentId)
            .HasMaxLength(255);

        builder.HasIndex(p => p.BookingId)
            .IsUnique();

        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
