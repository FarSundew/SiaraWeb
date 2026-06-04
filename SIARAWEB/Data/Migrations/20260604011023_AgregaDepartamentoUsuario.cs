using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIARAWEB.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregaDepartamentoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "AspNetUsers");
        }
    }
}
