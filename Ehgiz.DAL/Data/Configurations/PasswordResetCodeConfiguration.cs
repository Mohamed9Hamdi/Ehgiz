using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
    {
        builder.ToTable("PasswordResetCodes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CodeHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(c => c.CodeHash)
            .IsUnique();

        builder.Property(c => c.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.ExpiresAt)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithMany(u => u.PasswordResetCodes)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
