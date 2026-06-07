using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Address)
            .HasMaxLength(500);

        builder.Property(u => u.City)
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasData(
            new User
            {
                Id = SeedData.Users.AhmadId,
                FullName = "Ahmad Hassan",
                Email = "ahmad.hassan@ehgiz.com",
                PhoneNumber = "+201001234567",
                ProfileImageUrl = "https://cdn.ehgiz.com/users/ahmad.jpg",
                Address = "12 Nile Street",
                City = "Cairo",
                CreatedAt = SeedData.SeedDate,
                IsActive = true
            },
            new User
            {
                Id = SeedData.Users.SaraId,
                FullName = "Sara Mohamed",
                Email = "sara.mohamed@ehgiz.com",
                PhoneNumber = "+201076543210",
                ProfileImageUrl = "https://cdn.ehgiz.com/users/sara.jpg",
                Address = "45 Garden City",
                City = "Giza",
                CreatedAt = SeedData.SeedDate,
                IsActive = true
            },
            new User
            {
                Id = SeedData.Users.OmarId,
                FullName = "Omar Ali",
                Email = "omar.ali@ehgiz.com",
                PhoneNumber = "+201112223344",
                ProfileImageUrl = "https://cdn.ehgiz.com/users/omar.jpg",
                Address = "8 Alexandria Road",
                City = "Alexandria",
                CreatedAt = SeedData.SeedDate,
                IsActive = true
            });
    }
}
