using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyBooker.Airline.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airlines",
                columns: table => new
                {
                    airline_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    iata_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    icao_code = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    logo_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    country = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    contact_email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airlines", x => x.airline_id);
                });

            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    airport_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    iata_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    icao_code = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    latitude = table.Column<double>(type: "float", nullable: false),
                    longitude = table.Column<double>(type: "float", nullable: false),
                    timezone = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airports", x => x.airport_id);
                });

            migrationBuilder.CreateTable(
                name: "airline_airports",
                columns: table => new
                {
                    airline_id = table.Column<int>(type: "int", nullable: false),
                    airport_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_airports", x => new { x.airline_id, x.airport_id });
                    table.ForeignKey(
                        name: "FK_airline_airports_airlines_airline_id",
                        column: x => x.airline_id,
                        principalTable: "airlines",
                        principalColumn: "airline_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_airline_airports_airports_airport_id",
                        column: x => x.airport_id,
                        principalTable: "airports",
                        principalColumn: "airport_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airline_airports_airport_id",
                table: "airline_airports",
                column: "airport_id");

            migrationBuilder.CreateIndex(
                name: "IX_airlines_iata_code",
                table: "airlines",
                column: "iata_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_airports_city",
                table: "airports",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "IX_airports_country",
                table: "airports",
                column: "country");

            migrationBuilder.CreateIndex(
                name: "IX_airports_iata_code",
                table: "airports",
                column: "iata_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airline_airports");

            migrationBuilder.DropTable(
                name: "airlines");

            migrationBuilder.DropTable(
                name: "airports");
        }
    }
}
