using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("SavedSearches");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SearchTerm)
            .HasMaxLength(200);

        builder.Property(s => s.Location)
            .HasMaxLength(200);

        builder.Property(s => s.MinPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.MaxPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.Condition)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => s.UserId);

        builder.HasOne(s => s.User)
            .WithMany(u => u.SavedSearches)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
