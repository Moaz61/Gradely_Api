using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gradely.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudentAndTeacherNullableSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_TeacherId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submission_Assignment_Student",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "Submissions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Submission_Assignment_Student",
                table: "Submissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true,
                filter: "[StudentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_TeacherId",
                table: "Assignments",
                column: "TeacherId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_StudentId",
                table: "Submissions",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_TeacherId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submission_Assignment_Student",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "Submissions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TeacherId",
                table: "Assignments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submission_Assignment_Student",
                table: "Submissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_TeacherId",
                table: "Assignments",
                column: "TeacherId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_StudentId",
                table: "Submissions",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
