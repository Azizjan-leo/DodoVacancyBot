using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Console.Advanced.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppFill_alue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "AppFIlls",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value",
                table: "AppFIlls");
        }
    }
}
