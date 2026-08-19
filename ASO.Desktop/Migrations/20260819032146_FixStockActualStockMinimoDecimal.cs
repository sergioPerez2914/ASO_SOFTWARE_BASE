using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class FixStockActualStockMinimoDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La tabla Inventarios se creó a mano con StockActual/StockMinimo como int, pero el
            // modelo C# ya pedía decimal(18,2) desde antes de la migración baseline (hay artículos
            // por metro, kilo o litro). int -> decimal(18,2) es un ensanchamiento seguro, sin
            // pérdida de datos.
            migrationBuilder.AlterColumn<decimal>(
                name: "StockActual",
                table: "Inventarios",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "StockMinimo",
                table: "Inventarios",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StockActual",
                table: "Inventarios",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "StockMinimo",
                table: "Inventarios",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
