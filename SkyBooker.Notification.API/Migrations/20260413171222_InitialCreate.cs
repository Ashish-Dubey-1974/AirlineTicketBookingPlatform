using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyBooker.Notification.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    recipient_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    related_booking_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_is_read",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_id",
                table: "notifications",
                column: "recipient_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_related_booking_id",
                table: "notifications",
                column: "related_booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_sent_at",
                table: "notifications",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_type",
                table: "notifications",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}
