using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class AddAlumniProfileMentoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlumniProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    About = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Skills = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsVisibleToStudents = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailableForMentoring = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumniProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlumniProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlumniMentoringRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlumniProfileId = table.Column<int>(type: "int", nullable: false),
                    StudentUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumniMentoringRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlumniMentoringRequests_AlumniProfiles_AlumniProfileId",
                        column: x => x.AlumniProfileId,
                        principalTable: "AlumniProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlumniMentoringRequests_AspNetUsers_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlumniMentoringRequests_AlumniProfileId_StudentUserId",
                table: "AlumniMentoringRequests",
                columns: new[] { "AlumniProfileId", "StudentUserId" },
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_AlumniMentoringRequests_StudentUserId",
                table: "AlumniMentoringRequests",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlumniProfiles_UserId",
                table: "AlumniProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlumniMentoringRequests");

            migrationBuilder.DropTable(
                name: "AlumniProfiles");
        }
    }
}
