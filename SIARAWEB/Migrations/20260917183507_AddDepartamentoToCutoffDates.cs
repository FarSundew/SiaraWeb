using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartamentoToCutoffDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CutoffDates",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoId",
                table: "CutoffDates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CutoffDates_DepartamentoId",
                table: "CutoffDates",
                column: "DepartamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_CutoffDates_Departamentos_DepartamentoId",
                table: "CutoffDates",
                column: "DepartamentoId",
                principalTable: "Departamentos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CutoffDates_Departamentos_DepartamentoId",
                table: "CutoffDates");

            migrationBuilder.DropIndex(
                name: "IX_CutoffDates_DepartamentoId",
                table: "CutoffDates");

            migrationBuilder.DropColumn(
                name: "DepartamentoId",
                table: "CutoffDates");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CutoffDates",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);
        }
    }
}
