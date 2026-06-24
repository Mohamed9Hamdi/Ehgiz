using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ehgiz.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addSomebookingConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRevenueLedgers_Bookings_BookingId",
                table: "PlatformRevenueLedgers");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRevenueLedgers_Bookings_BookingId",
                table: "PlatformRevenueLedgers",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRevenueLedgers_Bookings_BookingId",
                table: "PlatformRevenueLedgers");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRevenueLedgers_Bookings_BookingId",
                table: "PlatformRevenueLedgers",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
