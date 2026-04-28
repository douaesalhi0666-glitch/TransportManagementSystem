using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemovePasswordColumnsFromPersonnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Transport");

            migrationBuilder.EnsureSchema(
                name: "Security");

            migrationBuilder.CreateTable(
                name: "Bus_tbl",
                schema: "Transport",
                columns: table => new
                {
                    Bus_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bus_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bus_PlateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bus_Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bus_Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Bus_Capacity = table.Column<int>(type: "int", nullable: true),
                    Bus_Year = table.Column<int>(type: "int", nullable: true),
                    Bus_Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bus_CurrentDriverId = table.Column<int>(type: "int", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "Driver_tbl",
                schema: "Security",
                columns: table => new
                {
                    Driver_id = table.Column<int>(type: "int", nullable: false),
                    Driver_FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Driver_LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Driver_PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Driver_Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Driver_LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Driver_LicenseExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Driver_ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    Driver_Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Driver_AssignedBusId = table.Column<int>(type: "int", nullable: true),
                    Driver_CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Driver_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Driver_PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Driver_ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Driver_ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Driver_EmailConfirmed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driver_tbl", x => x.Driver_id);
                });

            migrationBuilder.CreateTable(
                name: "Personnel_tbl",
                schema: "Security",
                columns: table => new
                {
                    Personnel_Id = table.Column<int>(type: "int", nullable: false),
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
                    Personnel_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel_tbl", x => x.Personnel_Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bus_tbl",
                schema: "Transport");

            migrationBuilder.DropTable(
                name: "Driver_tbl",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "Personnel_tbl",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "Trajectory_tbl",
                schema: "Transport");
        }
    }
}
