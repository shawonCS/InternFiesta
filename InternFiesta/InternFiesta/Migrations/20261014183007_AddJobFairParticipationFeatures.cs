using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class AddJobFairParticipationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAtUtc",
                table: "JobFairRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobFairBooths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobFairId = table.Column<int>(type: "int", nullable: false),
                    CompanyUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BoothName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BoothLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFairBooths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobFairBooths_AspNetUsers_CompanyUserId",
                        column: x => x.CompanyUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobFairBooths_JobFairs_JobFairId",
                        column: x => x.JobFairId,
                        principalTable: "JobFairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobFairFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobFairRegistrationId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFairFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobFairFeedbacks_JobFairRegistrations_JobFairRegistrationId",
                        column: x => x.JobFairRegistrationId,
                        principalTable: "JobFairRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobFairBooths_CompanyUserId",
                table: "JobFairBooths",
                column: "CompanyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobFairBooths_JobFairId_CompanyUserId",
                table: "JobFairBooths",
                columns: new[] { "JobFairId", "CompanyUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobFairFeedbacks_JobFairRegistrationId",
                table: "JobFairFeedbacks",
                column: "JobFairRegistrationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobFairBooths");

            migrationBuilder.DropTable(
                name: "JobFairFeedbacks");

            migrationBuilder.DropColumn(
                name: "AttendedAtUtc",
                table: "JobFairRegistrations");
        }
    }
}
