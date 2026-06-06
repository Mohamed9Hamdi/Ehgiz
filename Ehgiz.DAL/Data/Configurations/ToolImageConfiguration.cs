using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class ToolImageConfiguration : IEntityTypeConfiguration<ToolImage>
{
    public void Configure(EntityTypeBuilder<ToolImage> builder)
    {
        builder.ToTable("ToolImages");

        builder.HasKey(ti => ti.Id);

        builder.Property(ti => ti.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(ti => ti.Tool)
            .WithMany(t => t.Images)
            .HasForeignKey(ti => ti.ToolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
