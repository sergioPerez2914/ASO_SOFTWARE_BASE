using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase4_ColeccionesAnidadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacturasCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DiasCredito = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCobro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasCliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fincas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoCam = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fincas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Liquidaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SujetoTipo = table.Column<int>(type: "int", nullable: false),
                    SujetoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SujetoNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PeriodoDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodoHasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemesaIdsIncluidas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liquidaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacturaClienteLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RemesaId = table.Column<int>(type: "int", nullable: false),
                    FincaNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Servicio = table.Column<int>(type: "int", nullable: false),
                    NucleoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Toneladas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TarifaMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacturaClienteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaClienteLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaClienteLinea_FacturasCliente_FacturaClienteId",
                        column: x => x.FacturaClienteId,
                        principalTable: "FacturasCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FincaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lote_Fincas_FincaId",
                        column: x => x.FincaId,
                        principalTable: "Fincas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiquidacionLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Concepto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Origen = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnidadTexto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TarifaMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EsDeduccion = table.Column<bool>(type: "bit", nullable: false),
                    LiquidacionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionLinea_Liquidaciones_LiquidacionId",
                        column: x => x.LiquidacionId,
                        principalTable: "Liquidaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tablon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LoteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tablon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tablon_Lote_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturaClienteLinea_FacturaClienteId",
                table: "FacturaClienteLinea",
                column: "FacturaClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionLinea_LiquidacionId",
                table: "LiquidacionLinea",
                column: "LiquidacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Lote_FincaId",
                table: "Lote",
                column: "FincaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tablon_LoteId",
                table: "Tablon",
                column: "LoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturaClienteLinea");

            migrationBuilder.DropTable(
                name: "LiquidacionLinea");

            migrationBuilder.DropTable(
                name: "Tablon");

            migrationBuilder.DropTable(
                name: "FacturasCliente");

            migrationBuilder.DropTable(
                name: "Liquidaciones");

            migrationBuilder.DropTable(
                name: "Lote");

            migrationBuilder.DropTable(
                name: "Fincas");
        }
    }
}
