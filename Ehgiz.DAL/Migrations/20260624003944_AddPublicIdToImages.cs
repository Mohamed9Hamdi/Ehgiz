using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ehgiz.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicIdToImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ToolImages",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "HandoverImages",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ToolImages");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "HandoverImages");
        }
    }
}
