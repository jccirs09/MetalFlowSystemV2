using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickingLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    PickingListNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PrintDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ShipDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Buyer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SalesRep = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ShipVia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SoldTo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShipTo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderInstructions = table.Column<string>(type: "TEXT", nullable: false),
                    TotalWeightLbs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingLists_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickingListLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PickingListId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderUnit = table.Column<string>(type: "TEXT", nullable: false),
                    WidthIn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LengthIn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineInstructions = table.Column<string>(type: "TEXT", nullable: false),
                    ProductionAreaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineType = table.Column<int>(type: "INTEGER", nullable: false),
                    LineStatus = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingListLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingListLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PickingListLines_PickingLists_PickingListId",
                        column: x => x.PickingListId,
                        principalTable: "PickingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickingListLines_ProductionAreas_ProductionAreaId",
                        column: x => x.ProductionAreaId,
                        principalTable: "ProductionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickingListLineReservedMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PickingListLineId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MillRef = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Size = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingListLineReservedMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingListLineReservedMaterials_PickingListLines_PickingListLineId",
                        column: x => x.PickingListLineId,
                        principalTable: "PickingListLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickingListLineReservedMaterials_PickingListLineId_TagNumber",
                table: "PickingListLineReservedMaterials",
                columns: new[] { "PickingListLineId", "TagNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingListLines_ItemId",
                table: "PickingListLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingListLines_PickingListId_LineNumber",
                table: "PickingListLines",
                columns: new[] { "PickingListId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingListLines_ProductionAreaId",
                table: "PickingListLines",
                column: "ProductionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingLists_BranchId_PickingListNumber",
                table: "PickingLists",
                columns: new[] { "BranchId", "PickingListNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickingListLineReservedMaterials");

            migrationBuilder.DropTable(
                name: "PickingListLines");

            migrationBuilder.DropTable(
                name: "PickingLists");
        }
    }
}
