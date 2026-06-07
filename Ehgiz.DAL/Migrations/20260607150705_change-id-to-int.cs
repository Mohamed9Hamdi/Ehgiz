using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ehgiz.DAL.Migrations
{
    /// <inheritdoc />
    public partial class changeidtoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_RenterId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_User1Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_User2Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueReports_AspNetUsers_ReporterId",
                table: "IssueReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_SenderId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Tools_AspNetUsers_OwnerId",
                table: "Tools");

            migrationBuilder.Sql("DELETE FROM [IssueReports]");
            migrationBuilder.Sql("DELETE FROM [Reviews]");
            migrationBuilder.Sql("DELETE FROM [Payments]");
            migrationBuilder.Sql("DELETE FROM [Messages]");
            migrationBuilder.Sql("DELETE FROM [Notifications]");
            migrationBuilder.Sql("DELETE FROM [Bookings]");
            migrationBuilder.Sql("DELETE FROM [Conversations]");
            migrationBuilder.Sql("DELETE FROM [ToolImages]");
            migrationBuilder.Sql("DELETE FROM [Tools]");
            migrationBuilder.Sql("DELETE FROM [AspNetUsers]");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "AspNetUsers",
                type: "int",
                nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.DropIndex(
                name: "IX_Tools_OwnerId",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Tools");

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Tools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RenterId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RenterId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "RenterId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_Conversations_User1Id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_User2Id",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User1Id",
                table: "Conversations");

            migrationBuilder.AddColumn<int>(
                name: "User1Id",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "User2Id",
                table: "Conversations");

            migrationBuilder.AddColumn<int>(
                name: "User2Id",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SenderId",
                table: "Messages");

            migrationBuilder.AddColumn<int>(
                name: "SenderId",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Notifications");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_IssueReports_ReporterId",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "ReporterId",
                table: "IssueReports");

            migrationBuilder.AddColumn<int>(
                name: "ReporterId",
                table: "IssueReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_RenterId",
                table: "Bookings",
                column: "RenterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_User1Id",
                table: "Conversations",
                column: "User1Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_User2Id",
                table: "Conversations",
                column: "User2Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IssueReports_AspNetUsers_ReporterId",
                table: "IssueReports",
                column: "ReporterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_AspNetUsers_SenderId",
                table: "Messages",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tools_AspNetUsers_OwnerId",
                table: "Tools",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("SET IDENTITY_INSERT [AspNetUsers] ON");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "Email", "FullName", "IsActive", "PhoneNumber", "ProfileImageUrl" },
                values: new object[,]
                {
                    { 1, "12 Nile Street", "Cairo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ahmad.hassan@ehgiz.com", "Ahmad Hassan", true, "+201001234567", "https://cdn.ehgiz.com/users/ahmad.jpg" },
                    { 2, "45 Garden City", "Giza", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sara.mohamed@ehgiz.com", "Sara Mohamed", true, "+201076543210", "https://cdn.ehgiz.com/users/sara.jpg" },
                    { 3, "8 Alexandria Road", "Alexandria", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "omar.ali@ehgiz.com", "Omar Ali", true, "+201112223344", "https://cdn.ehgiz.com/users/omar.jpg" }
                });

            migrationBuilder.Sql("SET IDENTITY_INSERT [AspNetUsers] OFF");

            migrationBuilder.InsertData(
                table: "Conversations",
                columns: new[] { "Id", "UpdatedAt", "User1Id", "User2Id" },
                values: new object[] { 1, new DateTime(2026, 3, 9, 16, 0, 0, 0, DateTimeKind.Utc), 1, 2 });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "Content", "CreatedAt", "IsRead", "Type", "UserId" },
                values: new object[] { 1, "Your booking for Bosch Professional Drill has been completed.", new DateTime(2026, 2, 4, 8, 0, 0, 0, DateTimeKind.Utc), true, "BookingUpdate", 2 });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "Content", "CreatedAt", "Type", "UserId" },
                values: new object[,]
                {
                    { 2, "Payment of 150 EGP is held in escrow for your ladder booking.", new DateTime(2026, 3, 9, 14, 35, 0, 0, DateTimeKind.Utc), "PaymentUpdate", 2 },
                    { 3, "You have a new message from Sara Mohamed.", new DateTime(2026, 3, 8, 11, 0, 0, 0, DateTimeKind.Utc), "NewMessage", 1 }
                });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "CategoryId", "Condition", "CreatedAt", "Description", "InsurancePrice", "IsAvailable", "Location", "Name", "OwnerId", "PricePerDay", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, "Good", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "18V cordless drill with two batteries and charger.", 150m, true, "Cairo, Maadi", "Bosch Professional Drill", 1, 75m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 2, "Excellent", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1600W electric lawn mower suitable for medium gardens.", 200m, true, "Cairo, Maadi", "Electric Lawn Mower", 1, 90m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "CategoryId", "Condition", "CreatedAt", "Description", "InsurancePrice", "IsAvailable", "Location", "Name", "OwnerId", "PricePerDay", "UpdatedAt" },
                values: new object[] { 3, 3, "Good", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "6-meter extension ladder with safety locks.", 100m, false, "Alexandria, Smouha", "Aluminum Extension Ladder", 3, 50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "CategoryId", "Condition", "CreatedAt", "Description", "InsurancePrice", "IsAvailable", "Location", "Name", "OwnerId", "PricePerDay", "UpdatedAt" },
                values: new object[] { 4, 4, "Good", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2000 PSI pressure washer for outdoor cleaning.", 120m, true, "Alexandria, Smouha", "Pressure Washer", 3, 65m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Bookings",
                columns: new[] { "Id", "CreatedAt", "EndDate", "RenterId", "StartDate", "Status", "ToolId", "TotalPrice" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 3, 0, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completed", 1, 225m },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Active", 3, 150m }
                });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "Id", "Content", "ConversationId", "CreatedAt", "SenderId", "Status" },
                values: new object[,]
                {
                    { 1, "Hi Ahmad, is the drill still available next week?", 1, new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), 2, "Read" },
                    { 2, "Yes, it is available. Let me know your dates.", 1, new DateTime(2026, 1, 20, 9, 15, 0, 0, DateTimeKind.Utc), 1, "Read" },
                    { 3, "Can I pick up the ladder on March 10th?", 1, new DateTime(2026, 3, 8, 11, 0, 0, 0, DateTimeKind.Utc), 2, "Delivered" }
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
                values: new object[] { 1, 2, new DateTime(2026, 3, 11, 10, 0, 0, 0, DateTimeKind.Utc), "One of the safety locks on the ladder feels loose.", 2, "Open", "Ladder lock issue" });

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
            throw new NotSupportedException("Reverting user ID type change is not supported.");
        }
    }
}
