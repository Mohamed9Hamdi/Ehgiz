using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class PlatformRevenueLedgerConfiguration : IEntityTypeConfiguration<PlatformRevenueLedger>
{
    public void Configure(EntityTypeBuilder<PlatformRevenueLedger> builder)
    {
        builder.ToTable("PlatformRevenueLedgers");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Amount)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(b => b.Booking)
            .WithMany()
            .HasForeignKey(b => b.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
