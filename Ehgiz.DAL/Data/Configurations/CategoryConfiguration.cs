using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasData(
            new Category
            {
                Id = 1,
                Name = "Power Tools",
                Description = "Drills, saws, sanders, and other electric tools.",
                ImageUrl = "https://cdn.ehgiz.com/categories/power-tools.jpg",
                IsActive = true
            },
            new Category
            {
                Id = 2,
                Name = "Gardening",
                Description = "Lawn mowers, trimmers, and garden equipment.",
                ImageUrl = "https://cdn.ehgiz.com/categories/gardening.jpg",
                IsActive = true
            },
            new Category
            {
                Id = 3,
                Name = "Construction",
                Description = "Ladders, scaffolding, and construction gear.",
                ImageUrl = "https://cdn.ehgiz.com/categories/construction.jpg",
                IsActive = true
            },
            new Category
            {
                Id = 4,
                Name = "Cleaning Equipment",
                Description = "Pressure washers, vacuums, and cleaning machines.",
                ImageUrl = "https://cdn.ehgiz.com/categories/cleaning.jpg",
                IsActive = true
            });
    }
}
