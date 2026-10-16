using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingCategoriesFriosAndPet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ShoppingCategories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000009"),
                column: "SortOrder",
                value: 110);

            migrationBuilder.InsertData(
                table: "ShoppingCategories",
                columns: new[] { "Id", "HomeId", "Name", "SortOrder", "IsActive" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000010"), new Guid("20000000-0000-4000-8000-000000000001"), "Frios e embutidos", 90, true },
                    { new Guid("10000000-0000-4000-8000-000000000011"), new Guid("20000000-0000-4000-8000-000000000001"), "pet", 100, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ShoppingCategories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "ShoppingCategories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000011"));

            migrationBuilder.UpdateData(
                table: "ShoppingCategories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000009"),
                column: "SortOrder",
                value: 90);
        }
    }
}
