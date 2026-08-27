using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase17_CostoUnitarioLubricante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "Lubricantes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "Lubricantes");
        }
    }
}
