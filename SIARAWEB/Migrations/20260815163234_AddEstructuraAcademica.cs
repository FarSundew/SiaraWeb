using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddEstructuraAcademica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartamentoId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DocenteAsignaturas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Departamentos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departamentos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "HeadOfDepartmentId",
                table: "Departamentos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_HeadOfDepartmentId",
                table: "Departamentos",
                column: "HeadOfDepartmentId",
                unique: true,
                filter: "[HeadOfDepartmentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Departamentos_AspNetUsers_HeadOfDepartmentId",
                table: "Departamentos",
                column: "HeadOfDepartmentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departamentos_AspNetUsers_HeadOfDepartmentId",
                table: "Departamentos");

            migrationBuilder.DropIndex(
                name: "IX_Departamentos_HeadOfDepartmentId",
                table: "Departamentos");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DocenteAsignaturas");

            migrationBuilder.DropColumn(
                name: "HeadOfDepartmentId",
                table: "Departamentos");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Departamentos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departamentos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartamentoId",
                table: "AspNetUsers",
                column: "DepartamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoId",
                table: "AspNetUsers",
                column: "DepartamentoId",
                principalTable: "Departamentos",
                principalColumn: "Id");
        }
    }
}
