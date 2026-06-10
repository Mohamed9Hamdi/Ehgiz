using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(w => w.HeldBalance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(w => w.UpdatedAt)
            .IsRequired();

        builder.HasIndex(w => w.UserId)
            .IsUnique();

        builder.HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
