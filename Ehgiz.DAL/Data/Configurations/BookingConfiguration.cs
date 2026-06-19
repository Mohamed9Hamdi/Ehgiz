using Ehgiz.DAL.Entities;
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

        builder.Property(b => b.AdminResolutionNotes)
            .HasMaxLength(2000);

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
    }
}
