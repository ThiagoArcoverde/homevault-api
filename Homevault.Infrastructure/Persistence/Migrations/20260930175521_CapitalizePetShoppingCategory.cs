using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CapitalizePetShoppingCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ShoppingCategories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000011"),
                column: "Name",
                value: "Pet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ShoppingCategories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000011"),
                column: "Name",
                value: "pet");
        }
    }
}
