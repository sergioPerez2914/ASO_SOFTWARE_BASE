using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase25_EnvasesEnCotizacionYOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Unidades",
                table: "OrdenCompraLinea",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Unidades",
                table: "CotizacionProveedorLinea",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unidades",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "Unidades",
                table: "CotizacionProveedorLinea");
        }
    }
}
