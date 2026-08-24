using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase9_RenombrarCisternaAStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renombrar en vez de recrear: es el mismo dato ("cisterna" no existe en la empresa,
            // pero las filas que ya se cargaron sí), solo cambia cómo se llama la tabla.
            migrationBuilder.RenameTable(
                name: "TanquesCombustible",
                newName: "StockCombustible");

            migrationBuilder.RenameColumn(
                name: "TanqueNombre",
                table: "ValesCombustible",
                newName: "StockCombustibleNombre");

            migrationBuilder.RenameColumn(
                name: "TanqueId",
                table: "ValesCombustible",
                newName: "StockCombustibleId");

            migrationBuilder.RenameColumn(
                name: "TanqueNombre",
                table: "RequisicionLinea",
                newName: "StockCombustibleNombre");

            migrationBuilder.RenameColumn(
                name: "TanqueId",
                table: "RequisicionLinea",
                newName: "StockCombustibleId");

            migrationBuilder.RenameColumn(
                name: "TanqueNombre",
                table: "RecargasCombustible",
                newName: "StockCombustibleNombre");

            migrationBuilder.RenameColumn(
                name: "TanqueId",
                table: "RecargasCombustible",
                newName: "StockCombustibleId");

            migrationBuilder.RenameColumn(
                name: "TanqueNombre",
                table: "OrdenCompraLinea",
                newName: "StockCombustibleNombre");

            migrationBuilder.RenameColumn(
                name: "TanqueId",
                table: "OrdenCompraLinea",
                newName: "StockCombustibleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "StockCombustible",
                newName: "TanquesCombustible");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleNombre",
                table: "ValesCombustible",
                newName: "TanqueNombre");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleId",
                table: "ValesCombustible",
                newName: "TanqueId");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleNombre",
                table: "RequisicionLinea",
                newName: "TanqueNombre");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleId",
                table: "RequisicionLinea",
                newName: "TanqueId");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleNombre",
                table: "RecargasCombustible",
                newName: "TanqueNombre");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleId",
                table: "RecargasCombustible",
                newName: "TanqueId");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleNombre",
                table: "OrdenCompraLinea",
                newName: "TanqueNombre");

            migrationBuilder.RenameColumn(
                name: "StockCombustibleId",
                table: "OrdenCompraLinea",
                newName: "TanqueId");
        }
    }
}
