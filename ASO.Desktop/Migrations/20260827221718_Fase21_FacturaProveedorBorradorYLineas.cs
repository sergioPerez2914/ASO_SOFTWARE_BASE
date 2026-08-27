using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase21_FacturaProveedorBorradorYLineas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacturaProveedorId",
                table: "OrdenesCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaVencimiento",
                table: "FacturasProveedor",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "OrdenCompraId",
                table: "FacturasProveedor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacturaProveedorLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestinoTexto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CantidadTexto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacturaProveedorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaProveedorLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaProveedorLinea_FacturasProveedor_FacturaProveedorId",
                        column: x => x.FacturaProveedorId,
                        principalTable: "FacturasProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturaProveedorLinea_FacturaProveedorId",
                table: "FacturaProveedorLinea",
                column: "FacturaProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturaProveedorLinea");

            migrationBuilder.DropColumn(
                name: "FacturaProveedorId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "OrdenCompraId",
                table: "FacturasProveedor");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaVencimiento",
                table: "FacturasProveedor",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
