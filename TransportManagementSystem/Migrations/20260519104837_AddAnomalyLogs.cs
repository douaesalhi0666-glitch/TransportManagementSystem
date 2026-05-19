using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAnomalyLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnomalyLog_tbl",
                schema: "Service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnomalyType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusId = table.Column<long>(type: "bigint", nullable: true),
                    PersonnelId = table.Column<long>(type: "bigint", nullable: true),
                    SeverityScore = table.Column<double>(type: "float", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyLog_tbl", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelBusAssignment_tbl",
                schema: "Assignment",
                columns: table => new
                {
                    PBA_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PBA_PersonnelId = table.Column<long>(type: "bigint", nullable: false),
                    PBA_BusId = table.Column<long>(type: "bigint", nullable: false),
                    PBA_AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PBA_UnassignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PBA_Status = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelBusAssignment_tbl", x => x.PBA_Id);
                    table.ForeignKey(
                        name: "FK_PersonnelBusAssignment_tbl_Bus_tbl_PBA_BusId",
                        column: x => x.PBA_BusId,
                        principalSchema: "Transport",
                        principalTable: "Bus_tbl",
                        principalColumn: "Bus_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelBusAssignment_tbl_Personnel_tbl_PBA_PersonnelId",
                        column: x => x.PBA_PersonnelId,
                        principalSchema: "Security",
                        principalTable: "Personnel_tbl",
                        principalColumn: "Personnel_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PBA_PersonnelBus_Status",
                schema: "Assignment",
                table: "PersonnelBusAssignment_tbl",
                columns: new[] { "PBA_PersonnelId", "PBA_BusId", "PBA_Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelBusAssignment_tbl_PBA_BusId",
                schema: "Assignment",
                table: "PersonnelBusAssignment_tbl",
                column: "PBA_BusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnomalyLog_tbl",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "PersonnelBusAssignment_tbl",
                schema: "Assignment");
        }
    }
}
