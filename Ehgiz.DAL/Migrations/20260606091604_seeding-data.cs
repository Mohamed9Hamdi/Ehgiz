using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ehgiz.DAL.Migrations
{
    /// <inheritdoc />
    public partial class seedingdata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "Email", "FullName", "IsActive", "PhoneNumber", "ProfileImageUrl" },
                values: new object[,]
                {
                    { "11111111-1111-1111-1111-111111111111", "12 Nile Street", "Cairo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ahmad.hassan@ehgiz.com", "Ahmad Hassan", true, "+201001234567", "https://cdn.ehgiz.com/users/ahmad.jpg" },
                    { "22222222-2222-2222-2222-222222222222", "45 Garden City", "Giza", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sara.mohamed@ehgiz.com", "Sara Mohamed", true, "+201076543210", "https://cdn.ehgiz.com/users/sara.jpg" },
                    { "33333333-3333-3333-3333-333333333333", "8 Alexandria Road", "Alexandria", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "omar.ali@ehgiz.com", "Omar Ali", true, "+201112223344", "https://cdn.ehgiz.com/users/omar.jpg" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "ImageUrl", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Drills, saws, sanders, and other electric tools.", "https://cdn.ehgiz.com/categories/power-tools.jpg", true, "Power Tools" },
                    { 2, "Lawn mowers, trimmers, and garden equipment.", "https://cdn.ehgiz.com/categories/gardening.jpg", true, "Gardening" },
                    { 3, "Ladders, scaffolding, and construction gear.", "https://cdn.ehgiz.com/categories/construction.jpg", true, "Construction" },
                    { 4, "Pressure washers, vacuums, and cleaning machines.", "https://cdn.ehgiz.com/categories/cleaning.jpg", true, "Cleaning Equipment" }
                });

            migrationBuilder.InsertData(
                table: "Conversations",
                columns: new[] { "Id", "UpdatedAt", "User1Id", "User2Id" },
                values: new object[] { 1, new DateTime(2026, 3, 9, 16, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222" });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "Content", "CreatedAt", "IsRead", "Type", "UserId" },
                values: new object[] { 1, "Your booking for Bosch Professional Drill has been completed.", new DateTime(2026, 2, 4, 8, 0, 0, 0, DateTimeKind.Utc), true, "BookingUpdate", "22222222-2222-2222-2222-222222222222" });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "Content", "CreatedAt", "Type", "UserId" },
                values: new object[,]
                {
                    { 2, "Payment of 150 EGP is held in escrow for your ladder booking.", new DateTime(2026, 3, 9, 14, 35, 0, 0, DateTimeKind.Utc), "PaymentUpdate", "22222222-2222-2222-2222-222222222222" },
                    { 3, "You have a new message from Sara Mohamed.", new DateTime(2026, 3, 8, 11, 0, 0, 0, DateTimeKind.Utc), "NewMessage", "11111111-1111-1111-1111-111111111111" }
                });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "CategoryId", "Condition", "CreatedAt", "Description", "InsurancePrice", "IsAvailable", "Location", "Name", "OwnerId", "PricePerDay", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, "Good", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "18V cordless drill with two batteries and charger.", 150m, true, "Cairo, Maadi", "Bosch Professional Drill", "11111111-1111-1111-1111-111111111111", 75m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 2, "Excellent", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1600W electric lawn mower suitable for medium gardens.", 200m, true, "Cairo, Maadi", "Electric Lawn Mower", "11111111-1111-1111-1111-111111111111", 90m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "CategoryId", "Condition", "CreatedAt", "Description", "InsurancePrice", "Location", "Name", "OwnerId", "PricePerDay", "UpdatedAt" },
                values: new object[] { 3, 3, "Good", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "6-meter extension ladder with safety locks.", 100m, "Alexandria, Smouha", "Aluminum Extension Ladder", "33333333-3333-3333-3333-333333333333", 50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "CategoryId", "Condition", "CreatedAt", "Description", "InsurancePrice", "IsAvailable", "Location", "Name", "OwnerId", "PricePerDay", "UpdatedAt" },
                values: new object[] { 4, 4, "Good", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2000 PSI pressure washer for outdoor cleaning.", 120m, true, "Alexandria, Smouha", "Pressure Washer", "33333333-3333-3333-3333-333333333333", 65m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Bookings",
                columns: new[] { "Id", "CreatedAt", "EndDate", "RenterId", "StartDate", "Status", "ToolId", "TotalPrice" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 3, 0, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completed", 1, 225m },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222", new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Active", 3, 150m }
                });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "Id", "Content", "ConversationId", "CreatedAt", "SenderId", "Status" },
                values: new object[,]
                {
                    { 1, "Hi Ahmad, is the drill still available next week?", 1, new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222", "Read" },
                    { 2, "Yes, it is available. Let me know your dates.", 1, new DateTime(2026, 1, 20, 9, 15, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111", "Read" },
                    { 3, "Can I pick up the ladder on March 10th?", 1, new DateTime(2026, 3, 8, 11, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222", "Delivered" }
                });

            migrationBuilder.InsertData(
                table: "ToolImages",
                columns: new[] { "Id", "ImageUrl", "ToolId" },
                values: new object[,]
                {
                    { 1, "https://cdn.ehgiz.com/tools/drill-1.jpg", 1 },
                    { 2, "https://cdn.ehgiz.com/tools/drill-2.jpg", 1 },
                    { 3, "https://cdn.ehgiz.com/tools/mower-1.jpg", 2 },
                    { 4, "https://cdn.ehgiz.com/tools/ladder-1.jpg", 3 },
                    { 5, "https://cdn.ehgiz.com/tools/washer-1.jpg", 4 }
                });

            migrationBuilder.InsertData(
                table: "IssueReports",
                columns: new[] { "Id", "BookingId", "CreatedAt", "Description", "ReporterId", "Status", "Title" },
                values: new object[] { 1, 2, new DateTime(2026, 3, 11, 10, 0, 0, 0, DateTimeKind.Utc), "One of the safety locks on the ladder feels loose.", "22222222-2222-2222-2222-222222222222", "Open", "Ladder lock issue" });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BookingId", "EscrowStatus", "PaidAt", "PaymentMethod", "PaymentStatus" },
                values: new object[,]
                {
                    { 1, 225m, 1, "Released", new DateTime(2026, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), "CreditCard", "Completed" },
                    { 2, 150m, 2, "Held", new DateTime(2026, 3, 9, 14, 30, 0, 0, DateTimeKind.Utc), "Wallet", "Completed" }
                });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "BookingId", "Comment", "CreatedAt", "Rating" },
                values: new object[] { 1, 1, "Excellent drill, worked perfectly for my home project.", new DateTime(2026, 2, 4, 12, 0, 0, 0, DateTimeKind.Utc), 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IssueReports",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Reviews",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ToolImages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ToolImages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ToolImages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ToolImages",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ToolImages",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Bookings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Bookings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Conversations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "22222222-2222-2222-2222-222222222222");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "33333333-3333-3333-3333-333333333333");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
