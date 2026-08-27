using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASO.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class Fase18_ClaseLubricanteEnRequisicion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaseLubricante",
                table: "RequisicionLinea",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaseLubricante",
                table: "RequisicionLinea");
        }
    }
}
