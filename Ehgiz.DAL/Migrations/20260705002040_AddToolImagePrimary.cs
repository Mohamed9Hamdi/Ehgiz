using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ehgiz.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddToolImagePrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "ToolImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill: mark the first image of every tool as primary.
            migrationBuilder.Sql("""
                UPDATE ti SET IsPrimary = 1
                FROM ToolImages ti
                WHERE ti.Id = (SELECT MIN(t2.Id) FROM ToolImages t2 WHERE t2.ToolId = ti.ToolId)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "ToolImages");
        }
    }
}
