using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBusCurrentTrajectoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropTable(
                name: "BusFragmentAssignment_tbl",
                schema: "Assignment");

            migrationBuilder.DropTable(
                name: "DriverFragmentAssignment_tbl",
                schema: "Assignment");

            migrationBuilder.DropTable(
                name: "FragmentStop_tbl",
                schema: "Transport");

            migrationBuilder.DropTable(
                name: "TrajectoryFragment_tbl",
                schema: "Transport");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_tbl_AssignedFragmentId",
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

            migrationBuilder.CreateTable(
                name: "MotorizationRequests_tbl",
                schema: "Service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonnelId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedIsMotorized = table.Column<bool>(type: "bit", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorizationRequests_tbl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotorizationRequests_tbl_Personnel_tbl_PersonnelId",
                        column: x => x.PersonnelId,
                        principalSchema: "Security",
                        principalTable: "Personnel_tbl",
                        principalColumn: "Personnel_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MotorizationRequests_tbl_PersonnelId",
                schema: "Service",
                table: "MotorizationRequests_tbl",
                column: "PersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bus_tbl_Trajectory_tbl_Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentTrajectoryId",
                principalSchema: "Transport",
                principalTable: "Trajectory_tbl",
                principalColumn: "Trajectory_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bus_tbl_Trajectory_tbl_Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.DropTable(
                name: "MotorizationRequests_tbl",
                schema: "Service");

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

            migrationBuilder.AddColumn<int>(
                name: "AssignedFragmentId",
                schema: "Security",
                table: "Personnel_tbl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bus_CurrentFragmentId",
                schema: "Transport",
                table: "Bus_tbl",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrajectoryFragment_tbl",
                schema: "Transport",
                columns: table => new
                {
                    Fragment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Trajectory_Id = table.Column<int>(type: "int", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    Fragment_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fragment_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    Total_Workers = table.Column<int>(type: "int", nullable: false)
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
                name: "BusFragmentAssignment_tbl",
                schema: "Assignment",
                columns: table => new
                {
                    Assignment_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bus_Id = table.Column<long>(type: "bigint", nullable: false),
                    Fragment_Id = table.Column<int>(type: "int", nullable: false),
                    End_DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Start_DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    End_DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Start_DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
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
        }
    }
}
