using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase6_OrganizacionYSeguridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: 1 no es decorativo. El filtro global por organizacion es fail-closed
            // (sin ambito no se ve nada), asi que si las filas existentes se quedaran en 0
            // desapareceria todo lo cargado hasta hoy. Van al nucleo 1, que se crea al final.

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "ValesCombustible",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Tarifas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "TanquesCombustible",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "SalidasInventario",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Remesas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "ReglasMantenimiento",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "RecargasCombustible",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Proveedores",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "PersonalCampo",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Nucleos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "MantenimientoRegistros",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Liquidaciones",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Jornadas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Inventarios",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Fincas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "FacturasProveedor",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "FacturasCliente",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "EventosOperacion",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Empleados",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "ConceptosNomina",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "ActivosFlota",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Organizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermisosUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Permiso = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Concedido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosUsuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeticionesCambio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Permiso = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TipoEntidad = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntidadDescripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    SolicitadoPorId = table.Column<int>(type: "int", nullable: false),
                    SolicitadoPorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SolicitadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResueltoPorId = table.Column<int>(type: "int", nullable: true),
                    ResueltoPorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ResueltoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComentarioResolucion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeticionesCambio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    NombreUsuario = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizaciones_Codigo",
                table: "Organizaciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermisosUsuario_UsuarioId_Permiso",
                table: "PermisosUsuario",
                columns: new[] { "UsuarioId", "Permiso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            // El nucleo al que pertenece todo lo que ya estaba. Se le pone un nombre provisional:
            // la pantalla de primera puesta en marcha lo renombra al del centro real.
            migrationBuilder.InsertData(
                table: "Organizaciones",
                columns: new[] { "Id", "Activa", "Codigo", "Nombre" },
                values: new object[] { 1, true, "NUC-1", "Núcleo inicial" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Organizaciones");

            migrationBuilder.DropTable(
                name: "PermisosUsuario");

            migrationBuilder.DropTable(
                name: "PeticionesCambio");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "ValesCombustible");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Tarifas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "TanquesCombustible");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "SalidasInventario");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "ReglasMantenimiento");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "RecargasCombustible");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "PersonalCampo");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Nucleos");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "MantenimientoRegistros");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Jornadas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Inventarios");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Fincas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "FacturasProveedor");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "FacturasCliente");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "EventosOperacion");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "ConceptosNomina");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "ActivosFlota");
        }
    }
}
