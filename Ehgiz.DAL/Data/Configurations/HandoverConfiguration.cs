using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class HandoverConfiguration : IEntityTypeConfiguration<Handover>
{
    public void Configure(EntityTypeBuilder<Handover> builder)
    {
        builder.ToTable("Handovers");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(h => h.SubmitterNotes)
            .HasMaxLength(1000);

        builder.Property(h => h.ResponderNotes)
            .HasMaxLength(1000);

        builder.Property(h => h.SubmittedAt)
            .IsRequired();

        builder.HasOne(h => h.Booking)
            .WithMany(b => b.Handovers)
            .HasForeignKey(h => h.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.SubmittedByUser)
            .WithMany()
            .HasForeignKey(h => h.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.RespondedByUser)
            .WithMany()
            .HasForeignKey(h => h.RespondedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
