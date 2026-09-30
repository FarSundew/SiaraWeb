using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInitialPhase",
                table: "CutoffDates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "AcademicTrackings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "AcademicTrackings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsInitialPhase",
                table: "CutoffDates");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AcademicTrackings");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "AcademicTrackings");
        }
    }
}
