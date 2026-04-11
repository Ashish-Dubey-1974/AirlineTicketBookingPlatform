
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyBooker.Bookings.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    booking_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    flight_id = table.Column<int>(type: "int", nullable: false),
                    pnr_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    trip_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    total_fare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    base_fare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    taxes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ancillary_charges = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    meal_preference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    luggage_kg = table.Column<int>(type: "int", nullable: false),
                    contact_email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    contact_phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    booked_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    confirmed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    payment_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    cancellation_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    refund_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    return_flight_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.booking_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_booked_at",
                table: "bookings",
                column: "booked_at");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_flight_id",
                table: "bookings",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_pnr_code",
                table: "bookings",
                column: "pnr_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_status",
                table: "bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_user_id",
                table: "bookings",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");
        }
    }
}
