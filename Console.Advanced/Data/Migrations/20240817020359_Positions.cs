using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Console.Advanced.Data.Migrations
{
    /// <inheritdoc />
    public partial class Positions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Vacancies");

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuName = table.Column<string>(type: "text", nullable: false),
                    KyName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "KyName", "RuName" },
                values: new object[,]
                {
                    { 1, "Пиццамейкер", "Пиццамейкер" },
                    { 2, "Кассир", "Кассир" },
                    { 3, "Курьер", "Курьер" },
                    { 4, "Клинер", "Клинер" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_PositionId",
                table: "Vacancies",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Positions_PositionId",
                table: "Vacancies",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Positions_PositionId",
                table: "Vacancies");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_PositionId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Vacancies");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
