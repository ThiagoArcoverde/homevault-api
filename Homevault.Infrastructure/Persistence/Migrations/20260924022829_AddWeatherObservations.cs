using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherObservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Latitude = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    RelativeHumidity = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherObservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservations_City_State_ObservedAt",
                table: "WeatherObservations",
                columns: new[] { "City", "State", "ObservedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherObservations");
        }
    }
}
