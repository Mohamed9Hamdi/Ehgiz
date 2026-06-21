using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class UserConnectionConfiguration : IEntityTypeConfiguration<UserConnection>
{
    public void Configure(EntityTypeBuilder<UserConnection> builder)
    {
        builder.ToTable("UserConnections");

        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.ConnectionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(uc => uc.ConnectedAt)
            .IsRequired();

        builder.Property(uc => uc.IsOnline)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(uc => uc.ConnectionId)
            .IsUnique();

        builder.HasOne(uc => uc.User)
            .WithMany(u => u.UserConnections)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
