using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Data.Migrations
{
    /// <inheritdoc />
    public partial class DepartamentoTableDocentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departamentos_DepartamentoAsignadoId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartamentoAsignadoId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "DepartamentoAsignadoId",
                table: "AspNetUsers",
                newName: "DepartamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DepartamentoId",
                table: "AspNetUsers",
                newName: "DepartamentoAsignadoId");

            migrationBuilder.AddColumn<int>(
                name: "Departamento",
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
    }
}
