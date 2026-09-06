using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FailurePercentage",
                table: "AcademicTrackings",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DropoutPercentage",
                table: "AcademicTrackings",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ApprovalPercentage",
                table: "AcademicTrackings",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "ApprovedStudents",
                table: "AcademicTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AcademicTrackings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DroppedStudents",
                table: "AcademicTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FailedStudents",
                table: "AcademicTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalStudents",
                table: "AcademicTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedStudents",
                table: "AcademicTrackings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AcademicTrackings");

            migrationBuilder.DropColumn(
                name: "DroppedStudents",
                table: "AcademicTrackings");

            migrationBuilder.DropColumn(
                name: "FailedStudents",
                table: "AcademicTrackings");

            migrationBuilder.DropColumn(
                name: "TotalStudents",
                table: "AcademicTrackings");

            migrationBuilder.AlterColumn<decimal>(
                name: "FailurePercentage",
                table: "AcademicTrackings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DropoutPercentage",
                table: "AcademicTrackings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ApprovalPercentage",
                table: "AcademicTrackings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");
        }
    }
}
