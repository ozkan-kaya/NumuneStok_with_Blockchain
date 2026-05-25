using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumuneStok.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Products",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Products");
        }
    }
}
