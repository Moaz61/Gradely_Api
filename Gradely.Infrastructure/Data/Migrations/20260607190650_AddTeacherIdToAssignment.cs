using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gradely.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherIdToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeacherId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: true);

            // Populate existing assignments with the seeded admin's ID (or the first user's ID)
            migrationBuilder.Sql("UPDATE Assignments SET TeacherId = (SELECT TOP 1 Id FROM AspNetUsers WHERE Email = 'admin@gradely.com') WHERE TeacherId IS NULL");
            migrationBuilder.Sql("UPDATE Assignments SET TeacherId = (SELECT TOP 1 Id FROM AspNetUsers) WHERE TeacherId IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TeacherId",
                table: "Assignments",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_TeacherId",
                table: "Assignments",
                column: "TeacherId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_TeacherId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TeacherId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Assignments");
        }
    }
}
