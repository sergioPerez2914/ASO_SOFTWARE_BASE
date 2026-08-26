using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase16_UnidadesLubricante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExistenciaL",
                table: "Lubricantes",
                newName: "Unidades");

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "Lubricantes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "Lubricantes");

            migrationBuilder.RenameColumn(
                name: "Unidades",
                table: "Lubricantes",
                newName: "ExistenciaL");
        }
    }
}
