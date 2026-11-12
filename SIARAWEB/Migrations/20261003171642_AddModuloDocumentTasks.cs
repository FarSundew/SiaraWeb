using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloDocumentTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Departamentos_HeadOfDepartmentId",
                table: "Departamentos");

            migrationBuilder.AddColumn<int>(
                name: "DocumentTaskId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentTaskId",
                table: "AcademicTrackings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcademicPeriodId = table.Column<int>(type: "int", nullable: false),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    PhaseType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTasks_AcademicPeriods_AcademicPeriodId",
                        column: x => x.AcademicPeriodId,
                        principalTable: "AcademicPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTasks_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentTaskId",
                table: "Documents",
                column: "DocumentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_HeadOfDepartmentId",
                table: "Departamentos",
                column: "HeadOfDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartamentoId",
                table: "AspNetUsers",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTrackings_DocumentTaskId",
                table: "AcademicTrackings",
                column: "DocumentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTasks_AcademicPeriodId",
                table: "DocumentTasks",
                column: "AcademicPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTasks_DepartamentoId",
                table: "DocumentTasks",
                column: "DepartamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicTrackings_DocumentTasks_DocumentTaskId",
                table: "AcademicTrackings",
                column: "DocumentTaskId",
                principalTable: "DocumentTasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoId",
                table: "AspNetUsers",
                column: "DepartamentoId",
                principalTable: "Departamentos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_DocumentTasks_DocumentTaskId",
                table: "Documents",
                column: "DocumentTaskId",
                principalTable: "DocumentTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicTrackings_DocumentTasks_DocumentTaskId",
                table: "AcademicTrackings");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_DocumentTasks_DocumentTaskId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentTasks");

            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentTaskId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Departamentos_HeadOfDepartmentId",
                table: "Departamentos");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartamentoId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AcademicTrackings_DocumentTaskId",
                table: "AcademicTrackings");

            migrationBuilder.DropColumn(
                name: "DocumentTaskId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentTaskId",
                table: "AcademicTrackings");

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_HeadOfDepartmentId",
                table: "Departamentos",
                column: "HeadOfDepartmentId",
                unique: true,
                filter: "[HeadOfDepartmentId] IS NOT NULL");
        }
    }
}
