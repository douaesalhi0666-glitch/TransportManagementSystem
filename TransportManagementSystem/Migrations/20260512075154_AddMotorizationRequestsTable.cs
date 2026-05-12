using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorizationRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationLog_tbl",
                schema: "Service");

            migrationBuilder.AddColumn<bool>(
                name: "IsMotorized",
                schema: "Security",
                table: "Personnel_tbl",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CurrentOccupancy",
                schema: "Transport",
                table: "Bus_tbl",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMotorized",
                schema: "Security",
                table: "Personnel_tbl");

            migrationBuilder.DropColumn(
                name: "CurrentOccupancy",
                schema: "Transport",
                table: "Bus_tbl");

            migrationBuilder.CreateTable(
                name: "RecommendationLog_tbl",
                schema: "Service",
                columns: table => new
                {
                    Recommendation_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Recommendation_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Recommended_BusId = table.Column<long>(type: "bigint", nullable: false),
                    Recommended_DriverId = table.Column<long>(type: "bigint", nullable: false),
                    Recommended_TrajectoryId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Was_Accepted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationLog_tbl", x => x.Recommendation_Id);
                });
        }
    }
}
