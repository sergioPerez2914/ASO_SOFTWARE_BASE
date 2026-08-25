using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase12_RecepcionMercancia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecepcionMercanciaId",
                table: "OrdenesCompra",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecepcionesMercancia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    ProveedorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RecibidoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionesMercancia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecepcionMercanciaLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoInsumo = table.Column<int>(type: "int", nullable: false),
                    TipoCombustibleSolicitado = table.Column<int>(type: "int", nullable: true),
                    TipoLubricante = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StockCombustibleId = table.Column<int>(type: "int", nullable: true),
                    StockCombustibleNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ArticuloCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ActivoId = table.Column<int>(type: "int", nullable: true),
                    ActivoEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CantidadPedida = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnidadTexto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecepcionMercanciaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionMercanciaLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepcionMercanciaLinea_RecepcionesMercancia_RecepcionMercanciaId",
                        column: x => x.RecepcionMercanciaId,
                        principalTable: "RecepcionesMercancia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionMercanciaLinea_RecepcionMercanciaId",
                table: "RecepcionMercanciaLinea",
                column: "RecepcionMercanciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecepcionMercanciaLinea");

            migrationBuilder.DropTable(
                name: "RecepcionesMercancia");

            migrationBuilder.DropColumn(
                name: "RecepcionMercanciaId",
                table: "OrdenesCompra");
        }
    }
}
