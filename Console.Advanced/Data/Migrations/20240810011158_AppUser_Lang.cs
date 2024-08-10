using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Console.Advanced.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppUser_Lang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lang",
                table: "Users");
        }
    }
}
