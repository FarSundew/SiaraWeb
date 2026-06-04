using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Data.Migrations
{
    /// <inheritdoc />
    public partial class DepartamentoDocente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Departamento",
                table: "AspNetUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoAsignadoId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartamentoAsignadoId",
                table: "AspNetUsers",
                column: "DepartamentoAsignadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoAsignadoId",
                table: "AspNetUsers",
                column: "DepartamentoAsignadoId",
                principalTable: "Departamentos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoAsignadoId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartamentoAsignadoId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DepartamentoAsignadoId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Departamento",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
