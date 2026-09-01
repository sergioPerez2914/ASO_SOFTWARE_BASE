using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase23_LubricanteEnLitros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La existencia dejó de derivarse de Unidades × litros-por-presentación (tabla fija
            // que ya no existe en el modelo) y pasó a capturarse directo en litros. Antes de
            // borrar las columnas viejas, se agrega ExistenciaL y se puebla con la misma cuenta
            // que hacía Lubricante.LitrosPorPresentacion, para no perder la existencia que ya
            // había — mismo criterio de conversión que usaba ComprasService.ConfirmarRecepcion.
            migrationBuilder.AddColumn<decimal>(
                name: "ExistenciaL",
                table: "Lubricantes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE Lubricantes
                SET ExistenciaL = Unidades * CASE Presentacion
                    WHEN 'Barril' THEN 208
                    WHEN 'Tambor/Cuñete' THEN 208
                    WHEN 'Caneca' THEN 20
                    WHEN 'Galón' THEN 3.785
                    ELSE 1
                END");

            // La identidad de Lubricante pasó de Marca+Clase+Grado+Presentación a solo
            // Marca+Clase+Grado (una sola fila de existencia por producto, como StockCombustible
            // con Diésel). Puede haber quedado más de una fila para el mismo Marca+Clase+Grado si
            // antes tenía presentaciones distintas — no se fusionan aquí, porque decidir qué
            // CostoUnitario prevalece no es un dato que la migración pueda inventar: revisar y
            // fusionar a mano en Inventario · Combustible · Lubricantes antes de repartir esta build.
            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "Unidades",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "Lubricantes");

            migrationBuilder.DropColumn(
                name: "Unidades",
                table: "Lubricantes");

            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "CotizacionProveedorLinea");

            migrationBuilder.DropColumn(
                name: "Unidades",
                table: "CotizacionProveedorLinea");

            migrationBuilder.AddColumn<decimal>(
                name: "LitrosPorEnvase",
                table: "RecepcionMercanciaLinea",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No es el inverso exacto: no hay forma de reconstruir en qué presentación estaba
            // repartida la existencia una vez fusionada por Marca+Clase+Grado. Unidades vuelve
            // con la misma existencia asumiendo presentación "Granel" (1 L = 1 unidad).
            migrationBuilder.DropColumn(
                name: "LitrosPorEnvase",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "Lubricantes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Granel");

            migrationBuilder.AddColumn<decimal>(
                name: "Unidades",
                table: "Lubricantes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE Lubricantes SET Unidades = ExistenciaL");

            migrationBuilder.DropColumn(
                name: "ExistenciaL",
                table: "Lubricantes");

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "OrdenCompraLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Unidades",
                table: "OrdenCompraLinea",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "CotizacionProveedorLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Unidades",
                table: "CotizacionProveedorLinea",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
