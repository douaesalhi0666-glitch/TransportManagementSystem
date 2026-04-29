using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Security");

            migrationBuilder.EnsureSchema(
                name: "Service");

            migrationBuilder.EnsureSchema(
                name: "Transport");

            migrationBuilder.EnsureSchema(
                name: "Assignment");

            migrationBuilder.CreateTable(
                name: "Admin_tbl",
                schema: "Security",
                columns: table => new
                {
                    Admin_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Admin_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Admin_PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Admin_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin_tbl", x => x.Admin_Id);
                });

            migrationBuilder.CreateTable(
                name: "Trajectory_tbl",
                schema: "Transport",
                columns: table => new
                {
                    Trajectory_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Trajectory_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Trajectory_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Trajectory_Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Trajectory_StartLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Trajectory_StartLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    Trajectory_EndLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Trajectory_EndLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    Trajectory_DistanceKm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Trajectory_EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Trajectory_Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Trajectory_CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Trajectory_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trajectory_tbl", x => x.Trajectory_Id);
                });

            migrationBuilder.CreateTable(
                name: "TrajectoryStop_tbl",
                schema: "Transport",
                columns: table => new
                {
                    TS_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TS_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    TS_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TS_OrderIndex = table.Column<int>(type: "int", nullable: false),
                    TS_Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TS_Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TS_PlannedArrivalTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    TS_PlannedDepartureTime = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrajectoryStop_tbl", x => x.TS_Id);
                    table.ForeignKey(
                        name: "FK_TrajectoryStop_tbl_Trajectory_tbl_TS_TrajectoryId",
                        column: x => x.TS_TrajectoryId,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alert_tbl",
                schema: "Service",
                columns: table => new
                {
                    Alert_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alert_PersonnelId = table.Column<long>(type: "bigint", nullable: false),
                    Alert_BusId = table.Column<long>(type: "bigint", nullable: false),
                    Alert_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    Alert_Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Alert_Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Alert_SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Alert_DeliveryChannel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Alert_Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alert_tbl", x => x.Alert_Id);
                    table.ForeignKey(
                        name: "FK_Alert_tbl_Trajectory_tbl_Alert_TrajectoryId",
                        column: x => x.Alert_TrajectoryId,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bus_tbl",
                schema: "Transport",
                columns: table => new
                {
                    Bus_Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bus_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bus_PlateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bus_Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bus_Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Bus_Capacity = table.Column<int>(type: "int", nullable: true),
                    Bus_Year = table.Column<int>(type: "int", nullable: true),
                    Bus_Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bus_CurrentDriverId = table.Column<long>(type: "bigint", nullable: true),
                    Bus_CurrentTrajectoryId = table.Column<int>(type: "int", nullable: true),
                    Bus_CurrentLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Bus_CurrentLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    Bus_LastLocationUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Bus_CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Bus_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bus_tbl", x => x.Bus_Id);
                    table.ForeignKey(
                        name: "FK_Bus_tbl_Trajectory_tbl_Bus_CurrentTrajectoryId",
                        column: x => x.Bus_CurrentTrajectoryId,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id");
                });

            migrationBuilder.CreateTable(
                name: "Driver_tbl",
                schema: "Security",
                columns: table => new
                {
                    Driver_id = table.Column<long>(type: "bigint", nullable: false),
                    Driver_FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Driver_LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Driver_PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Driver_Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Driver_LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Driver_LicenseExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Driver_ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    Driver_Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Driver_AssignedBusId = table.Column<long>(type: "bigint", nullable: true),
                    Driver_CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Driver_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driver_tbl", x => x.Driver_id);
                    table.ForeignKey(
                        name: "FK_Driver_tbl_Bus_tbl_Driver_AssignedBusId",
                        column: x => x.Driver_AssignedBusId,
                        principalSchema: "Transport",
                        principalTable: "Bus_tbl",
                        principalColumn: "Bus_Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Personnel_tbl",
                schema: "Security",
                columns: table => new
                {
                    Personnel_Id = table.Column<long>(type: "bigint", nullable: false),
                    Personnel_FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Personnel_LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Personnel_Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Personnel_DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Personnel_PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Personnel_Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Personnel_EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Personnel_Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Personnel_Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Personnel_Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Personnel_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Personnel_Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Personnel_Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Personnel_CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Personnel_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HomeAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedTrajectoryId = table.Column<int>(type: "int", nullable: true),
                    AssignedBusId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel_tbl", x => x.Personnel_Id);
                    table.ForeignKey(
                        name: "FK_Personnel_tbl_Bus_tbl_AssignedBusId",
                        column: x => x.AssignedBusId,
                        principalSchema: "Transport",
                        principalTable: "Bus_tbl",
                        principalColumn: "Bus_Id");
                    table.ForeignKey(
                        name: "FK_Personnel_tbl_Trajectory_tbl_AssignedTrajectoryId",
                        column: x => x.AssignedTrajectoryId,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id");
                });

            migrationBuilder.CreateTable(
                name: "PersonnelTrajectoryAssignments_tbl",
                schema: "Assignment",
                columns: table => new
                {
                    PTA_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTA_PersonnelId = table.Column<long>(type: "bigint", nullable: false),
                    PTA_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    PTA_StopId = table.Column<int>(type: "int", nullable: true),
                    PTA_EffectiveFromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PTA_EffectiveToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PTA_Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelTrajectoryAssignments_tbl", x => x.PTA_Id);
                    table.ForeignKey(
                        name: "FK_PersonnelTrajectoryAssignments_tbl_Personnel_tbl_PTA_PersonnelId",
                        column: x => x.PTA_PersonnelId,
                        principalSchema: "Security",
                        principalTable: "Personnel_tbl",
                        principalColumn: "Personnel_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelTrajectoryAssignments_tbl_TrajectoryStop_tbl_PTA_StopId",
                        column: x => x.PTA_StopId,
                        principalSchema: "Transport",
                        principalTable: "TrajectoryStop_tbl",
                        principalColumn: "TS_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelTrajectoryAssignments_tbl_Trajectory_tbl_PTA_TrajectoryId",
                        column: x => x.PTA_TrajectoryId,
                        principalSchema: "Transport",
                        principalTable: "Trajectory_tbl",
                        principalColumn: "Trajectory_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alert_tbl_Alert_BusId",
                schema: "Service",
                table: "Alert_tbl",
                column: "Alert_BusId");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_tbl_Alert_PersonnelId",
                schema: "Service",
                table: "Alert_tbl",
                column: "Alert_PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_tbl_Alert_TrajectoryId",
                schema: "Service",
                table: "Alert_tbl",
                column: "Alert_TrajectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Bus_tbl_Bus_CurrentDriverId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Bus_tbl_Bus_CurrentTrajectoryId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentTrajectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Driver_tbl_Driver_AssignedBusId",
                schema: "Security",
                table: "Driver_tbl",
                column: "Driver_AssignedBusId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_tbl_AssignedBusId",
                schema: "Security",
                table: "Personnel_tbl",
                column: "AssignedBusId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_tbl_AssignedTrajectoryId",
                schema: "Security",
                table: "Personnel_tbl",
                column: "AssignedTrajectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTrajectoryAssignments_tbl_PTA_PersonnelId",
                schema: "Assignment",
                table: "PersonnelTrajectoryAssignments_tbl",
                column: "PTA_PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTrajectoryAssignments_tbl_PTA_StopId",
                schema: "Assignment",
                table: "PersonnelTrajectoryAssignments_tbl",
                column: "PTA_StopId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTrajectoryAssignments_tbl_PTA_TrajectoryId",
                schema: "Assignment",
                table: "PersonnelTrajectoryAssignments_tbl",
                column: "PTA_TrajectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TrajectoryStop_tbl_TS_TrajectoryId",
                schema: "Transport",
                table: "TrajectoryStop_tbl",
                column: "TS_TrajectoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alert_tbl_Bus_tbl_Alert_BusId",
                schema: "Service",
                table: "Alert_tbl",
                column: "Alert_BusId",
                principalSchema: "Transport",
                principalTable: "Bus_tbl",
                principalColumn: "Bus_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Alert_tbl_Personnel_tbl_Alert_PersonnelId",
                schema: "Service",
                table: "Alert_tbl",
                column: "Alert_PersonnelId",
                principalSchema: "Security",
                principalTable: "Personnel_tbl",
                principalColumn: "Personnel_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bus_tbl_Driver_tbl_Bus_CurrentDriverId",
                schema: "Transport",
                table: "Bus_tbl",
                column: "Bus_CurrentDriverId",
                principalSchema: "Security",
                principalTable: "Driver_tbl",
                principalColumn: "Driver_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Driver_tbl_Bus_tbl_Driver_AssignedBusId",
                schema: "Security",
                table: "Driver_tbl");

            migrationBuilder.DropTable(
                name: "Admin_tbl",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "Alert_tbl",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "PersonnelTrajectoryAssignments_tbl",
                schema: "Assignment");

            migrationBuilder.DropTable(
                name: "Personnel_tbl",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "TrajectoryStop_tbl",
                schema: "Transport");

            migrationBuilder.DropTable(
                name: "Bus_tbl",
                schema: "Transport");

            migrationBuilder.DropTable(
                name: "Driver_tbl",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "Trajectory_tbl",
                schema: "Transport");
        }
    }
}
