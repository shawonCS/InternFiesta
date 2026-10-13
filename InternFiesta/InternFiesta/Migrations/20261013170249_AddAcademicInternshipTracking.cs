using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicInternshipTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicInternships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InternshipApplicationId = table.Column<int>(type: "int", nullable: false),
                    FacultyUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicInternships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicInternships_AspNetUsers_FacultyUserId",
                        column: x => x.FacultyUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicInternships_InternshipApplications_InternshipApplicationId",
                        column: x => x.InternshipApplicationId,
                        principalTable: "InternshipApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicCompanyFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicInternshipId = table.Column<int>(type: "int", nullable: false),
                    CompanyUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicCompanyFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicCompanyFeedbacks_AcademicInternships_AcademicInternshipId",
                        column: x => x.AcademicInternshipId,
                        principalTable: "AcademicInternships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcademicCompanyFeedbacks_AspNetUsers_CompanyUserId",
                        column: x => x.CompanyUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicInternshipId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicMilestones_AcademicInternships_AcademicInternshipId",
                        column: x => x.AcademicInternshipId,
                        principalTable: "AcademicInternships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademicProgressUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicInternshipId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicProgressUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicProgressUpdates_AcademicInternships_AcademicInternshipId",
                        column: x => x.AcademicInternshipId,
                        principalTable: "AcademicInternships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCompanyFeedbacks_AcademicInternshipId",
                table: "AcademicCompanyFeedbacks",
                column: "AcademicInternshipId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCompanyFeedbacks_CompanyUserId",
                table: "AcademicCompanyFeedbacks",
                column: "CompanyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicInternships_FacultyUserId",
                table: "AcademicInternships",
                column: "FacultyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicInternships_InternshipApplicationId",
                table: "AcademicInternships",
                column: "InternshipApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicMilestones_AcademicInternshipId",
                table: "AcademicMilestones",
                column: "AcademicInternshipId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicProgressUpdates_AcademicInternshipId",
                table: "AcademicProgressUpdates",
                column: "AcademicInternshipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicCompanyFeedbacks");

            migrationBuilder.DropTable(
                name: "AcademicMilestones");

            migrationBuilder.DropTable(
                name: "AcademicProgressUpdates");

            migrationBuilder.DropTable(
                name: "AcademicInternships");
        }
    }
}
