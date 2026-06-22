using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class HandoverImageConfiguration : IEntityTypeConfiguration<HandoverImage>
{
    public void Configure(EntityTypeBuilder<HandoverImage> builder)
    {
        builder.ToTable("HandoverImages");

        builder.HasKey(hi => hi.Id);

        builder.Property(hi => hi.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(hi => hi.Caption)
            .HasMaxLength(300);

        builder.Property(hi => hi.UploadedAt)
            .IsRequired();

        builder.HasOne(hi => hi.Handover)
            .WithMany(h => h.Images)
            .HasForeignKey(hi => hi.HandoverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
