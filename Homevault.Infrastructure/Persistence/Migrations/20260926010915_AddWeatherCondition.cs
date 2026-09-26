using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "WeatherObservations",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<bool>(
                name: "IsDay",
                table: "WeatherObservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "WeatherObservations");

            migrationBuilder.DropColumn(
                name: "IsDay",
                table: "WeatherObservations");
        }
    }
}
