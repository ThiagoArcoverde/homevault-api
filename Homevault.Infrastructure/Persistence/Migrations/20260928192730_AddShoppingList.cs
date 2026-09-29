using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Homes",
                columns: new[] { "Id", "Name", "CreatedAt" },
                values: new object[] { new Guid("20000000-0000-4000-8000-000000000001"), "Casa padrão", new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero) });

            migrationBuilder.CreateTable(
                name: "ShoppingCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCategories", x => x.Id);
                    table.UniqueConstraint("AK_ShoppingCategories_HomeId_Id", x => new { x.HomeId, x.Id });
                    table.CheckConstraint("CK_ShoppingCategory_Name", "length(trim(Name)) BETWEEN 1 AND 40");
                    table.ForeignKey(
                        name: "FK_ShoppingCategories_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Purchased = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingItems", x => x.Id);
                    table.CheckConstraint("CK_ShoppingItem_Name", "length(trim(Name)) BETWEEN 1 AND 80");
                    table.CheckConstraint("CK_ShoppingItem_Quantity", "Quantity BETWEEN 1 AND 999");
                    table.ForeignKey(
                        name: "FK_ShoppingItems_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShoppingItems_ShoppingCategories_HomeId_CategoryId",
                        columns: x => new { x.HomeId, x.CategoryId },
                        principalTable: "ShoppingCategories",
                        principalColumns: new[] { "HomeId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCategories_HomeId_SortOrder_Name",
                table: "ShoppingCategories",
                columns: new[] { "HomeId", "SortOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_HomeId_CategoryId",
                table: "ShoppingItems",
                columns: new[] { "HomeId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_HomeId_CreatedAtUtc_Id",
                table: "ShoppingItems",
                columns: new[] { "HomeId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_HomeId_Purchased",
                table: "ShoppingItems",
                columns: new[] { "HomeId", "Purchased" });

            var homeId = new Guid("20000000-0000-4000-8000-000000000001");
            var names = new[]
            {
                "Hortifruti", "Mercearia", "Açougue e Peixaria", "Higiene e Beleza",
                "Laticínios", "Limpeza", "Bebida", "Padaria e Confeitaria", "Outros"
            };
            for (var index = 0; index < names.Length; index++)
            {
                migrationBuilder.InsertData(
                    table: "ShoppingCategories",
                    columns: new[] { "Id", "HomeId", "Name", "SortOrder", "IsActive" },
                    values: new object[]
                    {
                        new Guid($"10000000-0000-4000-8000-{index + 1:000000000000}"),
                        homeId, names[index], (index + 1) * 10, true
                    });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingItems");

            migrationBuilder.DropTable(
                name: "ShoppingCategories");

            migrationBuilder.DeleteData(
                table: "Homes",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000001"));
        }
    }
}
