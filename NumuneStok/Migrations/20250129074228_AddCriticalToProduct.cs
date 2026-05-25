using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumuneStok.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Critical",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MultiplicationValue",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Critical",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MultiplicationValue",
                table: "Products");
        }
    }
}
