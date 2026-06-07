using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .HasColumnType("text");

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Message
            {
                Id = 1,
                ConversationId = 1,
                SenderId = SeedData.Users.SaraId,
                Content = "Hi Ahmad, is the drill still available next week?",
                Status = MessageStatus.Read,
                CreatedAt = new DateTime(2026, 1, 20, 9, 0, 0, DateTimeKind.Utc)
            },
            new Message
            {
                Id = 2,
                ConversationId = 1,
                SenderId = SeedData.Users.AhmadId,
                Content = "Yes, it is available. Let me know your dates.",
                Status = MessageStatus.Read,
                CreatedAt = new DateTime(2026, 1, 20, 9, 15, 0, DateTimeKind.Utc)
            },
            new Message
            {
                Id = 3,
                ConversationId = 1,
                SenderId = SeedData.Users.SaraId,
                Content = "Can I pick up the ladder on March 10th?",
                Status = MessageStatus.Delivered,
                CreatedAt = new DateTime(2026, 3, 8, 11, 0, 0, DateTimeKind.Utc)
            });
    }
}
