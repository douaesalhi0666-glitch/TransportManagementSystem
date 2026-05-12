using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorizationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MotorizationRequests_tbl",
                schema: "Service");
        }
    }
}
