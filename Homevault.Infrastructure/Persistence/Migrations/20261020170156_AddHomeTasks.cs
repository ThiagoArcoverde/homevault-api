using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homevault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Meta = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Assignee = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    SortOrder = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeTasks", x => x.Id);
                    table.CheckConstraint("CK_HomeTask_Assignee", "length(trim(Assignee)) BETWEEN 1 AND 40");
                    table.CheckConstraint("CK_HomeTask_Meta", "length(trim(Meta)) BETWEEN 1 AND 60");
                    table.CheckConstraint("CK_HomeTask_SortOrder", "SortOrder > 0");
                    table.CheckConstraint("CK_HomeTask_Title", "length(trim(Title)) BETWEEN 1 AND 120");
                    table.ForeignKey(
                        name: "FK_HomeTasks_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeTasks_HomeId_SortOrder",
                table: "HomeTasks",
                columns: new[] { "HomeId", "SortOrder" },
                unique: true);

            var homeId = new Guid("20000000-0000-4000-8000-000000000001");
            migrationBuilder.InsertData(
                table: "HomeTasks",
                columns: new[] { "Id", "HomeId", "Title", "Meta", "Assignee", "Done", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-4000-8000-000000000001"), homeId, "Levar o lixo para fora", "Hoje, até 20:00", "Alex", false, 1L },
                    { new Guid("30000000-0000-4000-8000-000000000002"), homeId, "Regar as plantas da varanda", "Hoje, até 18:00", "Rafa", true, 2L },
                    { new Guid("30000000-0000-4000-8000-000000000003"), homeId, "Conferir a lista de compras", "Sem prazo", "Todos", false, 3L },
                    { new Guid("30000000-0000-4000-8000-000000000004"), homeId, "Limpar o filtro do ar-condicionado", "Amanhã", "Casa", false, 4L },
                    { new Guid("30000000-0000-4000-8000-000000000005"), homeId, "Organizar a bancada da cozinha", "Amanhã", "Alex", false, 5L },
                    { new Guid("30000000-0000-4000-8000-000000000006"), homeId, "Conferir a caixa de correio", "Sem prazo", "Rafa", false, 6L },
                    { new Guid("30000000-0000-4000-8000-000000000007"), homeId, "Recolher as roupas do varal", "Sábado", "Todos", false, 7L },
                    { new Guid("30000000-0000-4000-8000-000000000008"), homeId, "Verificar o filtro de água", "Segunda-feira", "Casa", false, 8L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeTasks");
        }
    }
}
