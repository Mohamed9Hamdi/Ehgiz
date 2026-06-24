using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ehgiz.DAL.Migrations
{
    /// <inheritdoc />
    public partial class BookingHandoverRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminResolutionNotes",
                table: "Bookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Handovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: false),
                    SubmitterNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedByUserId = table.Column<int>(type: "int", nullable: true),
                    ResponderNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Handovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Handovers_AspNetUsers_RespondedByUserId",
                        column: x => x.RespondedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Handovers_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Handovers_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoverImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HandoverId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverImages_Handovers_HandoverId",
                        column: x => x.HandoverId,
                        principalTable: "Handovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HandoverImages_HandoverId",
                table: "HandoverImages",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_Handovers_BookingId",
                table: "Handovers",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Handovers_RespondedByUserId",
                table: "Handovers",
                column: "RespondedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Handovers_SubmittedByUserId",
                table: "Handovers",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandoverImages");

            migrationBuilder.DropTable(
                name: "Handovers");

            migrationBuilder.DropColumn(
                name: "AdminResolutionNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Bookings");
        }
    }
}
