using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class FixTruckForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trucks_AspNetUsers_AssignedDriverId",
                table: "Trucks");

            migrationBuilder.DropIndex(
                name: "IX_Trucks_AssignedDriverId",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "Trucks");

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_AssignedDriverUserId",
                table: "Trucks",
                column: "AssignedDriverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trucks_AspNetUsers_AssignedDriverUserId",
                table: "Trucks",
                column: "AssignedDriverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trucks_AspNetUsers_AssignedDriverUserId",
                table: "Trucks");

            migrationBuilder.DropIndex(
                name: "IX_Trucks_AssignedDriverUserId",
                table: "Trucks");

            migrationBuilder.AddColumn<string>(
                name: "AssignedDriverId",
                table: "Trucks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_AssignedDriverId",
                table: "Trucks",
                column: "AssignedDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trucks_AspNetUsers_AssignedDriverId",
                table: "Trucks",
                column: "AssignedDriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
