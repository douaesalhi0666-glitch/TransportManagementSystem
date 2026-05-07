using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixTrajectoryColumnMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bus_tbl_Trajectory_tbl_Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.RenameColumn(
                name: "Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl",
                newName: "Trajectory_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Bus_tbl_Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl",
                newName: "IX_Bus_tbl_Trajectory_Id");

            migrationBuilder.AlterColumn<decimal>(
                name: "TS_Longitude",
                schema: "Transport",
                table: "TrajectoryStop_tbl",
                type: "decimal(11,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TS_Latitude",
                schema: "Transport",
                table: "TrajectoryStop_tbl",
                type: "decimal(10,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedStopId",
                schema: "Security",
                table: "Personnel_tbl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAssigned",
                schema: "Security",
                table: "Personnel_tbl",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusTrajectoryAssignment_tbl",
                schema: "Assignment",
                columns: table => new
                {
                    BTA_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BTA_BusId = table.Column<long>(type: "bigint", nullable: false),
                    BTA_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    BTA_StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BTA_EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BTA_Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusTrajectoryAssignment_tbl", x => x.BTA_Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverMissions_tbl",
                schema: "Service",
                columns: table => new
                {
                    Mission_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Driver_Id = table.Column<long>(type: "bigint", nullable: false),
                    Bus_Id = table.Column<long>(type: "bigint", nullable: false),
                    Mission_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalWorkers = table.Column<int>(type: "int", nullable: false),
                    WorkersDropped = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverMissions_tbl", x => x.Mission_Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverPerformance_tbl",
                schema: "Service",
                columns: table => new
                {
                    Performance_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Driver_Id = table.Column<long>(type: "bigint", nullable: false),
                    Trajectory_Id = table.Column<int>(type: "int", nullable: false),
                    TotalTrips = table.Column<int>(type: "int", nullable: false),
                    OnTimeTrips = table.Column<int>(type: "int", nullable: false),
                    AverageDelayMinutes = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LastTripDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPerformance_tbl", x => x.Performance_Id);
                    table.ForeignKey(
                        name: "FK_DriverPerformance_tbl_Driver_tbl_Driver_Id",
                        column: x => x.Driver_Id,
                        principalSchema: "Security",
                        principalTable: "Driver_tbl",
                        principalColumn: "Driver_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverPerformance_tbl_Trajectory_tbl_Trajectory_Id",
                        column: x => x.Trajectory_Id,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationLog_tbl",
                schema: "Service",
                columns: table => new
                {
                    Recommendation_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Recommendation_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Recommended_DriverId = table.Column<long>(type: "bigint", nullable: false),
                    Recommended_BusId = table.Column<long>(type: "bigint", nullable: false),
                    Recommended_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Was_Accepted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationLog_tbl", x => x.Recommendation_Id);
                });

            migrationBuilder.CreateTable(
                name: "TrajectoryFragment_tbl",
                schema: "Transport",
                columns: table => new
                {
                    Fragment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Trajectory_Id = table.Column<int>(type: "int", nullable: false),
                    Fragment_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fragment_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Total_Workers = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrajectoryFragment_tbl", x => x.Fragment_Id);
                    table.ForeignKey(
                        name: "FK_TrajectoryFragment_tbl_Trajectory_tbl_Trajectory_Id",
                        column: x => x.Trajectory_Id,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrajectoryShedule_tbl",
                schema: "Transport",
                columns: table => new
                {
                    TSched_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TSched_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    TSched_DayOfWeek = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TSched_DepartureTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    TSched_ReturnTime = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrajectoryShedule_tbl", x => x.TSched_Id);
                });

            migrationBuilder.CreateTable(
                name: "BusFragmentAssignment_tbl",
                schema: "Assignment",
                columns: table => new
                {
                    Assignment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bus_Id = table.Column<long>(type: "bigint", nullable: false),
                    Fragment_Id = table.Column<int>(type: "int", nullable: false),
                    Start_DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End_DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusFragmentAssignment_tbl", x => x.Assignment_Id);
                    table.ForeignKey(
                        name: "FK_BusFragmentAssignment_tbl_Bus_tbl_Bus_Id",
                        column: x => x.Bus_Id,
                        principalSchema: "Transport",
                        principalTable: "Bus_tbl",
                        principalColumn: "Bus_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusFragmentAssignment_tbl_TrajectoryFragment_tbl_Fragment_Id",
                        column: x => x.Fragment_Id,
                        principalSchema: "Transport",
                        principalTable: "TrajectoryFragment_tbl",
                        principalColumn: "Fragment_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverFragmentAssignment_tbl",
                schema: "Assignment",
                columns: table => new
                {
                    Assignment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Driver_Id = table.Column<long>(type: "bigint", nullable: false),
                    Fragment_Id = table.Column<int>(type: "int", nullable: false),
                    Start_DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End_DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverFragmentAssignment_tbl", x => x.Assignment_Id);
                    table.ForeignKey(
                        name: "FK_DriverFragmentAssignment_tbl_Driver_tbl_Driver_Id",
                        column: x => x.Driver_Id,
                        principalSchema: "Security",
                        principalTable: "Driver_tbl",
                        principalColumn: "Driver_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverFragmentAssignment_tbl_TrajectoryFragment_tbl_Fragment_Id",
                        column: x => x.Fragment_Id,
                        principalSchema: "Transport",
                        principalTable: "TrajectoryFragment_tbl",
                        principalColumn: "Fragment_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FragmentStop_tbl",
                schema: "Transport",
                columns: table => new
                {
                    Stop_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fragment_Id = table.Column<int>(type: "int", nullable: false),
                    TS_Id = table.Column<int>(type: "int", nullable: false),
                    Stop_Order = table.Column<int>(type: "int", nullable: false),
                    Workers_At_Stop = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FragmentStop_tbl", x => x.Stop_Id);
                    table.ForeignKey(
                        name: "FK_FragmentStop_tbl_TrajectoryFragment_tbl_Fragment_Id",
                        column: x => x.Fragment_Id,
                        principalSchema: "Transport",
                        principalTable: "TrajectoryFragment_tbl",
                        principalColumn: "Fragment_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FragmentStop_tbl_TrajectoryStop_tbl_TS_Id",
                        column: x => x.TS_Id,
                        principalSchema: "Transport",
                        principalTable: "TrajectoryStop_tbl",
                        principalColumn: "TS_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_tbl_AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl",
                column: "AssignedFragmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_tbl_AssignedStopId",
                schema: "Security",
                table: "Personnel_tbl",
                column: "AssignedStopId");

            migrationBuilder.CreateIndex(
                name: "IX_Bus_tbl_Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentFragmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BusFragmentAssignment_tbl_Bus_Id",
                schema: "Assignment",
                table: "BusFragmentAssignment_tbl",
                column: "Bus_Id");

            migrationBuilder.CreateIndex(
                name: "IX_BusFragmentAssignment_tbl_Fragment_Id",
                schema: "Assignment",
                table: "BusFragmentAssignment_tbl",
                column: "Fragment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DriverFragmentAssignment_tbl_Driver_Id",
                schema: "Assignment",
                table: "DriverFragmentAssignment_tbl",
                column: "Driver_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DriverFragmentAssignment_tbl_Fragment_Id",
                schema: "Assignment",
                table: "DriverFragmentAssignment_tbl",
                column: "Fragment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPerformance_tbl_Driver_Id",
                schema: "Service",
                table: "DriverPerformance_tbl",
                column: "Driver_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPerformance_tbl_Trajectory_Id",
                schema: "Service",
                table: "DriverPerformance_tbl",
                column: "Trajectory_Id");

            migrationBuilder.CreateIndex(
                name: "IX_FragmentStop_tbl_Fragment_Id",
                schema: "Transport",
                table: "FragmentStop_tbl",
                column: "Fragment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_FragmentStop_tbl_TS_Id",
                schema: "Transport",
                table: "FragmentStop_tbl",
                column: "TS_Id");

            migrationBuilder.CreateIndex(
                name: "IX_TrajectoryFragment_tbl_Trajectory_Id",
                schema: "Transport",
                table: "TrajectoryFragment_tbl",
                column: "Trajectory_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bus_tbl_TrajectoryFragment_tbl_Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentFragmentId",
                principalSchema: "Transport",
                principalTable: "TrajectoryFragment_tbl",
                principalColumn: "Fragment_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bus_tbl_Trajectory_tbl_Trajectory_Id",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Trajectory_Id",
                principalSchema: "Transport",
                principalTable: "Trajectory_tbl",
                principalColumn: "Trajectory_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_tbl_TrajectoryFragment_tbl_AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl",
                column: "AssignedFragmentId",
                principalSchema: "Transport",
                principalTable: "TrajectoryFragment_tbl",
                principalColumn: "Fragment_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_tbl_TrajectoryStop_tbl_AssignedStopId",
                schema: "Security",
                table: "Personnel_tbl",
                column: "AssignedStopId",
                principalSchema: "Transport",
                principalTable: "TrajectoryStop_tbl",
                principalColumn: "TS_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bus_tbl_TrajectoryFragment_tbl_Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.DropForeignKey(
                name: "FK_Bus_tbl_Trajectory_tbl_Trajectory_Id",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_tbl_TrajectoryFragment_tbl_AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_tbl_TrajectoryStop_tbl_AssignedStopId",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropTable(
                name: "BusFragmentAssignment_tbl",
                schema: "Assignment");

            migrationBuilder.DropTable(
                name: "BusTrajectoryAssignment_tbl",
                schema: "Assignment");

            migrationBuilder.DropTable(
                name: "DriverFragmentAssignment_tbl",
                schema: "Assignment");

            migrationBuilder.DropTable(
                name: "DriverMissions_tbl",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "DriverPerformance_tbl",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "FragmentStop_tbl",
                schema: "Transport");

            migrationBuilder.DropTable(
                name: "RecommendationLog_tbl",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "TrajectoryShedule_tbl",
                schema: "Transport");

            migrationBuilder.DropTable(
                name: "TrajectoryFragment_tbl",
                schema: "Transport");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_tbl_AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_tbl_AssignedStopId",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropIndex(
                name: "IX_Bus_tbl_Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.DropColumn(
                name: "AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropColumn(
                name: "AssignedStopId",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropColumn(
                name: "IsAssigned",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropColumn(
                name: "Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.RenameColumn(
                name: "Trajectory_Id",
                schema: "Transport",
                table: "Bus_tbl",
                newName: "Bus_CurrentTrajectoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Bus_tbl_Trajectory_Id",
                schema: "Transport",
                table: "Bus_tbl",
                newName: "IX_Bus_tbl_Bus_CurrentTrajectoryId");

            migrationBuilder.AlterColumn<decimal>(
                name: "TS_Longitude",
                schema: "Transport",
                table: "TrajectoryStop_tbl",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TS_Latitude",
                schema: "Transport",
                table: "TrajectoryStop_tbl",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)");

            migrationBuilder.AddForeignKey(
                name: "FK_Bus_tbl_Trajectory_tbl_Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentTrajectoryId",
                principalSchema: "Transport",
                principalTable: "Trajectory_tbl",
                principalColumn: "Trajectory_Id");
        }
    }
}
