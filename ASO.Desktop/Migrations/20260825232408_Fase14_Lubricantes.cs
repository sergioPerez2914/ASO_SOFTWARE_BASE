using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase14_Lubricantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LubricanteId",
                table: "RecepcionMercanciaLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LubricanteNombre",
                table: "RecepcionMercanciaLinea",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Lubricantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GradoViscosidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExistenciaL = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lubricantes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lubricantes");

            migrationBuilder.DropColumn(
                name: "LubricanteId",
                table: "RecepcionMercanciaLinea");

            migrationBuilder.DropColumn(
                name: "LubricanteNombre",
                table: "RecepcionMercanciaLinea");
        }
    }
}
