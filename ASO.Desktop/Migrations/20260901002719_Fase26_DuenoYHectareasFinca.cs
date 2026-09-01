using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase26_DuenoYHectareasFinca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dueno",
                table: "Fincas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Hectareas",
                table: "Fincas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dueno",
                table: "Fincas");

            migrationBuilder.DropColumn(
                name: "Hectareas",
                table: "Fincas");
        }
    }
}
