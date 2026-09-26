using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternFiesta.Migrations
{
    /// <inheritdoc />
    public partial class LinkApplicationsToStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StudentUserId",
                table: "InternshipApplications",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_InternshipApplications_StudentUserId",
                table: "InternshipApplications",
                column: "StudentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InternshipApplications_AspNetUsers_StudentUserId",
                table: "InternshipApplications",
                column: "StudentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InternshipApplications_AspNetUsers_StudentUserId",
                table: "InternshipApplications");

            migrationBuilder.DropIndex(
                name: "IX_InternshipApplications_StudentUserId",
                table: "InternshipApplications");

            migrationBuilder.AlterColumn<string>(
                name: "StudentUserId",
                table: "InternshipApplications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
