using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class LimpiezaFasesCutoffDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInitialPhase",
                table: "CutoffDates");

            migrationBuilder.DropColumn(
                name: "PhaseNumber",
                table: "CutoffDates");

            migrationBuilder.AddColumn<string>(
                name: "PhaseType",
                table: "CutoffDates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhaseType",
                table: "CutoffDates");

            migrationBuilder.AddColumn<bool>(
                name: "IsInitialPhase",
                table: "CutoffDates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PhaseNumber",
                table: "CutoffDates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
