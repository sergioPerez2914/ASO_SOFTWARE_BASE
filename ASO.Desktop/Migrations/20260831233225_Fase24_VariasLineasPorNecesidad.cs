using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase24_VariasLineasPorNecesidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LitrosPorEnvase",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioUnitario",
                table: "RecepcionMercanciaLinea",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Antes de este cambio, ConfirmarRecepcion buscaba el precio en la orden de compra por
            // Marca+Clase+Grado en vez de traerlo copiado; para no perder ese precio en las
            // recepciones de lubricante que ya estén en Borrador (aún no confirmadas), se hace ese
            // mismo cálculo una vez aquí y se deja copiado en la línea.
            migrationBuilder.Sql(@"
                UPDATE rml
                SET rml.PrecioUnitario = ocl.PrecioUnitario
                FROM RecepcionMercanciaLinea rml
                JOIN RecepcionesMercancia rm ON rm.Id = rml.RecepcionMercanciaId
                JOIN OrdenCompraLinea ocl ON ocl.OrdenCompraId = rm.OrdenCompraId
                    AND ocl.MarcaLubricanteId = rml.MarcaLubricanteId
                    AND ocl.ClaseLubricante = rml.ClaseLubricante
                    AND ocl.TipoLubricante = rml.TipoLubricante
                WHERE rml.MarcaLubricanteId IS NOT NULL");

            migrationBuilder.AddColumn<decimal>(
                name: "LitrosPorEnvase",
                table: "OrdenCompraLinea",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "OrdenCompraLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LitrosPorEnvase",
                table: "CotizacionProveedorLinea",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "CotizacionProveedorLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequisicionLineaIndex",
                table: "CotizacionProveedorLinea",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioUnitario",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.DropColumn(
                name: "LitrosPorEnvase",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "LitrosPorEnvase",
                table: "CotizacionProveedorLinea");

            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "CotizacionProveedorLinea");

            migrationBuilder.DropColumn(
                name: "RequisicionLineaIndex",
                table: "CotizacionProveedorLinea");

            migrationBuilder.AddColumn<decimal>(
                name: "LitrosPorEnvase",
                table: "RecepcionMercanciaLinea",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
