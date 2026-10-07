using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class ModuloClassroomTareasFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId",
                table: "DocenteAsignaturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_AcademicPeriods_AcademicPeriodId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_AcademicPeriodId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DocenteAsignaturas");

            migrationBuilder.AddColumn<int>(
                name: "AcademicPeriodId1",
                table: "DocenteAsignaturas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocenteAsignaturas_AcademicPeriodId1",
                table: "DocenteAsignaturas",
                column: "AcademicPeriodId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId",
                table: "DocenteAsignaturas",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId1",
                table: "DocenteAsignaturas",
                column: "AcademicPeriodId1",
                principalTable: "AcademicPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId",
                table: "DocenteAsignaturas");

            migrationBuilder.DropForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId1",
                table: "DocenteAsignaturas");

            migrationBuilder.DropIndex(
                name: "IX_DocenteAsignaturas_AcademicPeriodId1",
                table: "DocenteAsignaturas");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId1",
                table: "DocenteAsignaturas");

            migrationBuilder.AddColumn<int>(
                name: "AcademicPeriodId",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DocenteAsignaturas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_AcademicPeriodId",
                table: "Subjects",
                column: "AcademicPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocenteAsignaturas_AcademicPeriods_AcademicPeriodId",
                table: "DocenteAsignaturas",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_AcademicPeriods_AcademicPeriodId",
                table: "Subjects",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
