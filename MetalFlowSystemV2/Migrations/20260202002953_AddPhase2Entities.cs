using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowSystemV2.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AreaShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductionAreaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedHeadcount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfirmedHeadcount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaShifts_AspNetUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AreaShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaShifts_ProductionAreas_ProductionAreaId",
                        column: x => x.ProductionAreaId,
                        principalTable: "ProductionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaShifts_Shifts_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackingStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingStations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StationShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    PackingStationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedHeadcount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfirmedHeadcount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationShifts_AspNetUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StationShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StationShifts_PackingStations_PackingStationId",
                        column: x => x.PackingStationId,
                        principalTable: "PackingStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StationShifts_Shifts_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWorkAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkMode = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductionAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    PackingStationId = table.Column<int>(type: "INTEGER", nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWorkAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWorkAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWorkAssignments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWorkAssignments_PackingStations_PackingStationId",
                        column: x => x.PackingStationId,
                        principalTable: "PackingStations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserWorkAssignments_ProductionAreas_ProductionAreaId",
                        column: x => x.ProductionAreaId,
                        principalTable: "ProductionAreas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserWorkAssignments_Shifts_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ShiftTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkMode = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductionAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    PackingStationId = table.Column<int>(type: "INTEGER", nullable: true),
                    AreaShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    StationShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HeadcountOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverrideReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_AreaShifts_AreaShiftId",
                        column: x => x.AreaShiftId,
                        principalTable: "AreaShifts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_PackingStations_PackingStationId",
                        column: x => x.PackingStationId,
                        principalTable: "PackingStations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_ProductionAreas_ProductionAreaId",
                        column: x => x.ProductionAreaId,
                        principalTable: "ProductionAreas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_Shifts_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftAttendances_StationShifts_StationShiftId",
                        column: x => x.StationShiftId,
                        principalTable: "StationShifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaShifts_BranchId",
                table: "AreaShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaShifts_ClosedByUserId",
                table: "AreaShifts",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaShifts_ProductionAreaId",
                table: "AreaShifts",
                column: "ProductionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaShifts_ShiftTemplateId",
                table: "AreaShifts",
                column: "ShiftTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingStations_BranchId",
                table: "PackingStations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_AreaShiftId",
                table: "ShiftAttendances",
                column: "AreaShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_BranchId",
                table: "ShiftAttendances",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_PackingStationId",
                table: "ShiftAttendances",
                column: "PackingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_ProductionAreaId",
                table: "ShiftAttendances",
                column: "ProductionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_ShiftTemplateId",
                table: "ShiftAttendances",
                column: "ShiftTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_StationShiftId",
                table: "ShiftAttendances",
                column: "StationShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAttendances_UserId",
                table: "ShiftAttendances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StationShifts_BranchId",
                table: "StationShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StationShifts_ClosedByUserId",
                table: "StationShifts",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StationShifts_PackingStationId",
                table: "StationShifts",
                column: "PackingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_StationShifts_ShiftTemplateId",
                table: "StationShifts",
                column: "ShiftTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkAssignments_BranchId",
                table: "UserWorkAssignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkAssignments_PackingStationId",
                table: "UserWorkAssignments",
                column: "PackingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkAssignments_ProductionAreaId",
                table: "UserWorkAssignments",
                column: "ProductionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkAssignments_ShiftTemplateId",
                table: "UserWorkAssignments",
                column: "ShiftTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkAssignments_UserId",
                table: "UserWorkAssignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftAttendances");

            migrationBuilder.DropTable(
                name: "UserWorkAssignments");

            migrationBuilder.DropTable(
                name: "AreaShifts");

            migrationBuilder.DropTable(
                name: "StationShifts");

            migrationBuilder.DropTable(
                name: "PackingStations");
        }
    }
}
