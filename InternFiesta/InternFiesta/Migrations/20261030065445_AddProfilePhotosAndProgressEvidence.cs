using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotosAndProgressEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoFileName",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicProgressAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgressUpdateId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicProgressAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicProgressAttachments_AcademicProgressUpdates_ProgressUpdateId",
                        column: x => x.ProgressUpdateId,
                        principalTable: "AcademicProgressUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicProgressAttachments_ProgressUpdateId",
                table: "AcademicProgressAttachments",
                column: "ProgressUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicProgressAttachments");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoFileName",
                table: "AspNetUsers");
        }
    }
}
