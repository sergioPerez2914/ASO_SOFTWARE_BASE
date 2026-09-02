using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase27_BoletoCentral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_Atr",
                table: "Remesas",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_DescuentoAdministracion",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_DescuentoAlzaEmpuje",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_DescuentoCorte",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_DescuentoInvestigacion",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_DescuentoRural",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_DescuentoTransporte",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_Fibra",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_MontoCanaEntregada",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Boleto_Numero",
                table: "Remesas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_Pureza",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_TrashMineral",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_TrashVegetal",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boleto_ValorLiquido",
                table: "Remesas",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Boleto_Atr",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_DescuentoAdministracion",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_DescuentoAlzaEmpuje",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_DescuentoCorte",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_DescuentoInvestigacion",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_DescuentoRural",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_DescuentoTransporte",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_Fibra",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_MontoCanaEntregada",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_Numero",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_Pureza",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_TrashMineral",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_TrashVegetal",
                table: "Remesas");

            migrationBuilder.DropColumn(
                name: "Boleto_ValorLiquido",
                table: "Remesas");
        }
    }
}
