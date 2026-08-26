using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase15_Banco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuentasBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NumeroCuenta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasBancarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosBanco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    CuentaNombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Origen = table.Column<int>(type: "int", nullable: false),
                    OrigenId = table.Column<int>(type: "int", nullable: true),
                    ContraparteId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaConciliacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConciliadoPorId = table.Column<int>(type: "int", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosBanco", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosBanco_CuentaId_Fecha",
                table: "MovimientosBanco",
                columns: new[] { "CuentaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosBanco_Origen_OrigenId",
                table: "MovimientosBanco",
                columns: new[] { "Origen", "OrigenId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasBancarias");

            migrationBuilder.DropTable(
                name: "MovimientosBanco");
        }
    }
}
