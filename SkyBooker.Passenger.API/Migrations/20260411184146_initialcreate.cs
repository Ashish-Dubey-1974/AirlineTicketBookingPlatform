using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyBooker.Passenger.API.Migrations
{
    /// <inheritdoc />
    public partial class initialcreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "passenger_info",
                columns: table => new
                {
                    passenger_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    title = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    passport_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nationality = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    passport_expiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    seat_id = table.Column<int>(type: "int", nullable: true),
                    seat_number = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ticket_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    passenger_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    checked_in = table.Column<bool>(type: "bit", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_info", x => x.passenger_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_passengers_booking_id",
                table: "passenger_info",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_passengers_passport_number",
                table: "passenger_info",
                column: "passport_number");

            migrationBuilder.CreateIndex(
                name: "IX_passengers_seat_id",
                table: "passenger_info",
                column: "seat_id");

            migrationBuilder.CreateIndex(
                name: "IX_passengers_ticket_number",
                table: "passenger_info",
                column: "ticket_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "passenger_info");
        }
    }
}
