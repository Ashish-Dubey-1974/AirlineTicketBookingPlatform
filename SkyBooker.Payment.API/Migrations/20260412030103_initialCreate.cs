using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyBooker.Payment.API.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    booking_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    payment_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    transaction_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    gateway_response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    refund_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.payment_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_booking_id",
                table: "payments",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payments_transaction_id",
                table: "payments",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_user_id",
                table: "payments",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");
        }
    }
}
