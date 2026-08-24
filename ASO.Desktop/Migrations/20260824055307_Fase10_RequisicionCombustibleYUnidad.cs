using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase10_RequisicionCombustibleYUnidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No son renombres: StockCombustibleId (Id de un StockCombustible) y
            // TipoCombustibleSolicitado (enum Diesel/Lubricante) son campos distintos que
            // coinciden por casualidad en tipo de columna. `dotnet ef migrations add` los
            // había emparejado como RenameColumn por esa coincidencia de forma; se reescribió
            // a mano como drop + add para no reinterpretar en silencio ningún dato existente.
            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Requisiciones");

            migrationBuilder.DropColumn(
                name: "StockCombustibleId",
                table: "RequisicionLinea");

            migrationBuilder.DropColumn(
                name: "StockCombustibleNombre",
                table: "RequisicionLinea");

            migrationBuilder.DropColumn(
                name: "StockCombustibleId",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "StockCombustibleNombre",
                table: "OrdenCompraLinea");

            migrationBuilder.AddColumn<int>(
                name: "TipoCombustibleSolicitado",
                table: "RequisicionLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoLubricante",
                table: "RequisicionLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActivoId",
                table: "RequisicionLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivoEtiqueta",
                table: "RequisicionLinea",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoCombustibleSolicitado",
                table: "OrdenCompraLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoLubricante",
                table: "OrdenCompraLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActivoId",
                table: "OrdenCompraLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivoEtiqueta",
                table: "OrdenCompraLinea",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoCombustibleSolicitado",
                table: "RequisicionLinea");

            migrationBuilder.DropColumn(
                name: "TipoLubricante",
                table: "RequisicionLinea");

            migrationBuilder.DropColumn(
                name: "ActivoId",
                table: "RequisicionLinea");

            migrationBuilder.DropColumn(
                name: "ActivoEtiqueta",
                table: "RequisicionLinea");

            migrationBuilder.DropColumn(
                name: "TipoCombustibleSolicitado",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "TipoLubricante",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "ActivoId",
                table: "OrdenCompraLinea");

            migrationBuilder.DropColumn(
                name: "ActivoEtiqueta",
                table: "OrdenCompraLinea");

            migrationBuilder.AddColumn<int>(
                name: "StockCombustibleId",
                table: "RequisicionLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockCombustibleNombre",
                table: "RequisicionLinea",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StockCombustibleId",
                table: "OrdenCompraLinea",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockCombustibleNombre",
                table: "OrdenCompraLinea",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Requisiciones",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
