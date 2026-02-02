using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingListLineWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LineWeightLbs",
                table: "PickingListLines",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineWeightLbs",
                table: "PickingListLines");
        }
    }
}
