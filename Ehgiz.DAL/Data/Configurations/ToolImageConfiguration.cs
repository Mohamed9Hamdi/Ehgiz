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

        builder.HasData(
            new ToolImage { Id = 1, ToolId = 1, ImageUrl = "https://cdn.ehgiz.com/tools/drill-1.jpg" },
            new ToolImage { Id = 2, ToolId = 1, ImageUrl = "https://cdn.ehgiz.com/tools/drill-2.jpg" },
            new ToolImage { Id = 3, ToolId = 2, ImageUrl = "https://cdn.ehgiz.com/tools/mower-1.jpg" },
            new ToolImage { Id = 4, ToolId = 3, ImageUrl = "https://cdn.ehgiz.com/tools/ladder-1.jpg" },
            new ToolImage { Id = 5, ToolId = 4, ImageUrl = "https://cdn.ehgiz.com/tools/washer-1.jpg" });
    }
}
