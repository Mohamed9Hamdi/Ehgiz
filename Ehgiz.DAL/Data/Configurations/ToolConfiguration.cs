using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.ToTable("Tools");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasColumnType("text");

        builder.Property(t => t.PricePerDay)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.InsurancePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.Condition)
            .HasMaxLength(50);

        builder.Property(t => t.Location)
            .HasMaxLength(200);

        builder.Property(t => t.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.HasOne(t => t.Owner)
            .WithMany(u => u.OwnedTools)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Tools)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Tool
            {
                Id = 1,
                OwnerId = SeedData.Users.AhmadId,
                CategoryId = 1,
                Name = "Bosch Professional Drill",
                Description = "18V cordless drill with two batteries and charger.",
                PricePerDay = 75m,
                InsurancePrice = 150m,
                Condition = "Good",
                Location = "Cairo, Maadi",
                IsAvailable = true,
                CreatedAt = SeedData.SeedDate,
                UpdatedAt = SeedData.SeedDate
            },
            new Tool
            {
                Id = 2,
                OwnerId = SeedData.Users.AhmadId,
                CategoryId = 2,
                Name = "Electric Lawn Mower",
                Description = "1600W electric lawn mower suitable for medium gardens.",
                PricePerDay = 90m,
                InsurancePrice = 200m,
                Condition = "Excellent",
                Location = "Cairo, Maadi",
                IsAvailable = true,
                CreatedAt = SeedData.SeedDate,
                UpdatedAt = SeedData.SeedDate
            },
            new Tool
            {
                Id = 3,
                OwnerId = SeedData.Users.OmarId,
                CategoryId = 3,
                Name = "Aluminum Extension Ladder",
                Description = "6-meter extension ladder with safety locks.",
                PricePerDay = 50m,
                InsurancePrice = 100m,
                Condition = "Good",
                Location = "Alexandria, Smouha",
                IsAvailable = false,
                CreatedAt = SeedData.SeedDate,
                UpdatedAt = SeedData.SeedDate
            },
            new Tool
            {
                Id = 4,
                OwnerId = SeedData.Users.OmarId,
                CategoryId = 4,
                Name = "Pressure Washer",
                Description = "2000 PSI pressure washer for outdoor cleaning.",
                PricePerDay = 65m,
                InsurancePrice = 120m,
                Condition = "Good",
                Location = "Alexandria, Smouha",
                IsAvailable = true,
                CreatedAt = SeedData.SeedDate,
                UpdatedAt = SeedData.SeedDate
            });
    }
}
