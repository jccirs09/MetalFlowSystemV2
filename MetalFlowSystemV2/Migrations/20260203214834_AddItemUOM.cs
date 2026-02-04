using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class AddItemUOM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PoundsPerSquareFoot",
                table: "Items",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UOM",
                table: "Items",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoundsPerSquareFoot",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UOM",
                table: "Items");
        }
    }
}
