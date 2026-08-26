using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase15_MarcaYPresentacionLubricante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Marca",
                table: "Lubricantes",
                newName: "MarcaLubricanteNombre");

            migrationBuilder.AddColumn<string>(
                name: "ClaseLubricante",
                table: "RecepcionMercanciaLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaLubricanteId",
                table: "RecepcionMercanciaLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarcaLubricanteNombre",
                table: "RecepcionMercanciaLinea",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "RecepcionMercanciaLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaseLubricante",
                table: "OrdenCompraLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaLubricanteId",
                table: "OrdenCompraLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarcaLubricanteNombre",
                table: "OrdenCompraLinea",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Presentacion",
                table: "OrdenCompraLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaLubricanteId",
                table: "Lubricantes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MarcasLubricante",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcasLubricante", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MarcasLubricante",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "PDV (PDVSA Vassa)" },
                    { 2, true, "Global Oil" },
                    { 3, true, "Mobil" },
                    { 4, true, "Castrol" },
                    { 5, true, "Chevron" },
                    { 6, true, "Shell" },
                    { 7, true, "Total" },
                    { 8, true, "Terpel" },
                    { 9, true, "Lukoil" },
                    { 10, true, "Mannol" },
                    { 11, true, "Valvoline" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarcasLubricante");

            migrationBuilder.DropColumn(
                name: "ClaseLubricante",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.DropColumn(
                name: "MarcaLubricanteId",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.DropColumn(
                name: "MarcaLubricanteNombre",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.DropColumn(
                name: "ClaseLubricante",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "MarcaLubricanteId",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "MarcaLubricanteNombre",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "Presentacion",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "MarcaLubricanteId",
                table: "Lubricantes");

            migrationBuilder.RenameColumn(
                name: "MarcaLubricanteNombre",
                table: "Lubricantes",
                newName: "Marca");
        }
    }
}
