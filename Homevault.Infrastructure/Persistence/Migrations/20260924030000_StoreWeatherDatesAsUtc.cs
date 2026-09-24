using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreWeatherDatesAsUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The SQLite column remains TEXT; UTC normalization is handled by the EF value converter.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The database schema is unchanged when reverting this migration.
        }
    }
}
