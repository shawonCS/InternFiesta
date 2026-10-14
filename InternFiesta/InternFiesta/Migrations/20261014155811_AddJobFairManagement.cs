using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class AddJobFairManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobFairs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByCoordinatorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFairs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobFairRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobFairId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParticipantRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFairRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobFairRegistrations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobFairRegistrations_JobFairs_JobFairId",
                        column: x => x.JobFairId,
                        principalTable: "JobFairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobFairRegistrations_JobFairId_UserId",
                table: "JobFairRegistrations",
                columns: new[] { "JobFairId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobFairRegistrations_UserId",
                table: "JobFairRegistrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobFairRegistrations");

            migrationBuilder.DropTable(
                name: "JobFairs");
        }
    }
}
