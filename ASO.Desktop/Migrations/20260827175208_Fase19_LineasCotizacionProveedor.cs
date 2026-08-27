using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase19_LineasCotizacionProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoTotal",
                table: "CotizacionesProveedor");

            migrationBuilder.CreateTable(
                name: "CotizacionProveedorLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoInsumo = table.Column<int>(type: "int", nullable: false),
                    TipoCombustibleSolicitado = table.Column<int>(type: "int", nullable: true),
                    TipoLubricante = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MarcaLubricanteId = table.Column<int>(type: "int", nullable: true),
                    MarcaLubricanteNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaseLubricante = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Presentacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ArticuloCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ActivoId = table.Column<int>(type: "int", nullable: true),
                    ActivoEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnidadTexto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CotizacionProveedorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotizacionProveedorLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotizacionProveedorLinea_CotizacionesProveedor_CotizacionProveedorId",
                        column: x => x.CotizacionProveedorId,
                        principalTable: "CotizacionesProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionProveedorLinea_CotizacionProveedorId",
                table: "CotizacionProveedorLinea",
                column: "CotizacionProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CotizacionProveedorLinea");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotal",
                table: "CotizacionesProveedor",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
