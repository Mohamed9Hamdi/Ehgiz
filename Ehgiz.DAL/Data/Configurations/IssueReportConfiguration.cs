using Ehgiz.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehgiz.DAL.Data.Configurations;

public class IssueReportConfiguration : IEntityTypeConfiguration<IssueReport>
{
    public void Configure(EntityTypeBuilder<IssueReport> builder)
    {
        builder.ToTable("IssueReports");

        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.ReporterId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(ir => ir.Title)
            .HasMaxLength(200);

        builder.Property(ir => ir.Description)
            .HasColumnType("text");

        builder.Property(ir => ir.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ir => ir.CreatedAt)
            .IsRequired();

        builder.HasOne(ir => ir.Booking)
            .WithMany(b => b.IssueReports)
            .HasForeignKey(ir => ir.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.Reporter)
            .WithMany(u => u.IssueReports)
            .HasForeignKey(ir => ir.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
