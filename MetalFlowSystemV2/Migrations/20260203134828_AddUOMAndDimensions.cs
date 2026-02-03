using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class AddUOMAndDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PoundsPerSquareFoot",
                table: "Items",
                type: "decimal(18, 4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UOM",
                table: "Items",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "InventoryStocks",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "InventoryStocks",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);
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

            migrationBuilder.DropColumn(
                name: "Length",
                table: "InventoryStocks");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "InventoryStocks");
        }
    }
}
