using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodoToDocenteAsignatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicPeriodId",
                table: "DocenteAsignaturas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocenteAsignaturas_AcademicPeriodId",
                table: "DocenteAsignaturas",
                column: "AcademicPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId",
                table: "DocenteAsignaturas",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId",
                table: "DocenteAsignaturas");

            migrationBuilder.DropIndex(
                name: "IX_DocenteAsignaturas_AcademicPeriodId",
                table: "DocenteAsignaturas");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId",
                table: "DocenteAsignaturas");
        }
    }
}
