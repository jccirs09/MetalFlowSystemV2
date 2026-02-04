using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackingEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StationShiftId = table.Column<int>(type: "INTEGER", nullable: false),
                    PickingListId = table.Column<int>(type: "INTEGER", nullable: false),
                    PackedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PackedWeightLbs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingEvents_PickingLists_PickingListId",
                        column: x => x.PickingListId,
                        principalTable: "PickingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackingEvents_StationShifts_StationShiftId",
                        column: x => x.StationShiftId,
                        principalTable: "StationShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackingEvents_PickingListId",
                table: "PackingEvents",
                column: "PickingListId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingEvents_StationShiftId",
                table: "PackingEvents",
                column: "StationShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackingEvents");
        }
    }
}
