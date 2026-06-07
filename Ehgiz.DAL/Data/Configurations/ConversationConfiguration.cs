using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.User1)
            .WithMany(u => u.ConversationsAsUser1)
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.User2)
            .WithMany(u => u.ConversationsAsUser2)
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
