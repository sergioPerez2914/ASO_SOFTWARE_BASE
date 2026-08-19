using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase3_DocumentosPlanos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacturasProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    ProveedorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasProveedor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jornadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoPersonal = table.Column<int>(type: "int", nullable: false),
                    PersonaId = table.Column<int>(type: "int", nullable: false),
                    PersonaNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CargoORol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NucleoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Turno = table.Column<int>(type: "int", nullable: false),
                    HoraEntrada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoraSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jornadas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MantenimientoRegistros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivoId = table.Column<int>(type: "int", nullable: false),
                    ActivoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActivoEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LecturaUso = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RepuestosUsados = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CostoRepuestos = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CostoManoObra = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RealizadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RemesaId = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoRegistros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecargasCombustible",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanqueId = table.Column<int>(type: "int", nullable: false),
                    TanqueNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Litros = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProveedorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecargasCombustible", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Remesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FincaId = table.Column<int>(type: "int", nullable: false),
                    FincaCodigoCam = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FincaNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LoteNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TablonNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoCosecha = table.Column<int>(type: "int", nullable: false),
                    OperadorId = table.Column<int>(type: "int", nullable: false),
                    OperadorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OperadorNucleoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TractoristaId = table.Column<int>(type: "int", nullable: false),
                    TractoristaNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TractoristaNucleoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChoferId = table.Column<int>(type: "int", nullable: false),
                    ChoferNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VehiculoId = table.Column<int>(type: "int", nullable: false),
                    VehiculoPlaca = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RemeseroId = table.Column<int>(type: "int", nullable: false),
                    RemeseroNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NucleoCorteCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NucleoAlzaEmpujeCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NucleoTransporteCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InicioCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LlegadaCentral = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PesoBrutoT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaraT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FacturaClienteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remesas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalidasInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArticuloCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ArticuloNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActivoId = table.Column<int>(type: "int", nullable: true),
                    ActivoEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MantenimientoId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StockForzado = table.Column<bool>(type: "bit", nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalidasInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValesCombustible",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanqueId = table.Column<int>(type: "int", nullable: false),
                    TanqueNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ActivoId = table.Column<int>(type: "int", nullable: false),
                    ActivoCodigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActivoEtiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EsTransporte = table.Column<bool>(type: "bit", nullable: false),
                    Litros = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Lectura = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ResponsableNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ConsumoPorUnidad = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PromedioHistorico = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AlertaConsumo = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValesCombustible", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturasProveedor");

            migrationBuilder.DropTable(
                name: "Jornadas");

            migrationBuilder.DropTable(
                name: "MantenimientoRegistros");

            migrationBuilder.DropTable(
                name: "RecargasCombustible");

            migrationBuilder.DropTable(
                name: "Remesas");

            migrationBuilder.DropTable(
                name: "SalidasInventario");

            migrationBuilder.DropTable(
                name: "ValesCombustible");
        }
    }
}
