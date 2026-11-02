using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAssignedTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedTaskId",
                table: "AcademicProgressUpdates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignedTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicInternshipId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignedTasks_AcademicInternships_AcademicInternshipId",
                        column: x => x.AcademicInternshipId,
                        principalTable: "AcademicInternships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicProgressUpdates_AssignedTaskId",
                table: "AcademicProgressUpdates",
                column: "AssignedTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedTasks_AcademicInternshipId",
                table: "AssignedTasks",
                column: "AcademicInternshipId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicProgressUpdates_AssignedTasks_AssignedTaskId",
                table: "AcademicProgressUpdates",
                column: "AssignedTaskId",
                principalTable: "AssignedTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicProgressUpdates_AssignedTasks_AssignedTaskId",
                table: "AcademicProgressUpdates");

            migrationBuilder.DropTable(
                name: "AssignedTasks");

            migrationBuilder.DropIndex(
                name: "IX_AcademicProgressUpdates_AssignedTaskId",
                table: "AcademicProgressUpdates");

            migrationBuilder.DropColumn(
                name: "AssignedTaskId",
                table: "AcademicProgressUpdates");
        }
    }
}
