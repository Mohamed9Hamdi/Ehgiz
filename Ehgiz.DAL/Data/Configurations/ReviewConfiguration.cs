using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasColumnType("text");

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Booking)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new Review
            {
                Id = 1,
                BookingId = 1,
                Rating = 5,
                Comment = "Excellent drill, worked perfectly for my home project.",
                CreatedAt = new DateTime(2026, 2, 4, 12, 0, 0, DateTimeKind.Utc)
            });
    }
}
