using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Console.Advanced.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserVacancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VacancyId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_VacancyId",
                table: "Users",
                column: "VacancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Vacancies_VacancyId",
                table: "Users",
                column: "VacancyId",
                principalTable: "Vacancies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Vacancies_VacancyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_VacancyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VacancyId",
                table: "Users");
        }
    }
}
