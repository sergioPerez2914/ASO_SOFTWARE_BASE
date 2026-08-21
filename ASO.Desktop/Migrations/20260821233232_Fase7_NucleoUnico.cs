using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase7_NucleoUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El C.O.D pasa a vivir en la Organizacion: una instalacion atiende a un solo
            // nucleo, asi que el catalogo de nucleos de productores deja de tener sentido.
            migrationBuilder.AddColumn<string>(
                name: "CodigoCam",
                table: "Organizaciones",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // Se rellena ANTES de borrar la tabla, para no perder el codigo real: si el nucleo
            // tenia exactamente una fila en Nucleos, ese es su C.O.D. Con cero o con varias no
            // hay una respuesta unica, asi que se cae al codigo interno y se corrige a mano
            // desde la pantalla de primer arranque.
            migrationBuilder.Sql(@"
                UPDATE o
                SET o.CodigoCam = ISNULL((
                        SELECT MIN(n.Codigo)
                        FROM Nucleos n
                        WHERE n.OrganizacionId = o.Id
                        HAVING COUNT(*) = 1
                    ), o.Codigo)
                FROM Organizaciones o;");

            migrationBuilder.DropTable(
                name: "Nucleos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoCam",
                table: "Organizaciones");

            migrationBuilder.CreateTable(
                name: "Nucleos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OrganizacionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nucleos", x => x.Id);
                });
        }
    }
}
