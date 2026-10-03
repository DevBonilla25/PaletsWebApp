using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaletsWebApp.Migrations
{
    public partial class CustodioAnteriorReclamo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserIdCustodioAnterior",
                table: "Detalles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoPaletAnterior",
                table: "Detalles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationUserIdCustodioAnterior",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "EstadoPaletAnterior",
                table: "Detalles");
        }
    }
}
