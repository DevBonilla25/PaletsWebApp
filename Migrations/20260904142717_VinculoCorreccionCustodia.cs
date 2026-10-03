using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaletsWebApp.Migrations
{
    public partial class VinculoCorreccionCustodia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdDetalleOrigen",
                table: "Detalles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalles_IdDetalleOrigen",
                table: "Detalles",
                column: "IdDetalleOrigen");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_palets' AND Descripcion = N'En revisión')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion)
                    VALUES ('estado_palets', 0, 0, N'En revisión');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Detalles_IdDetalleOrigen",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "IdDetalleOrigen",
                table: "Detalles");

            migrationBuilder.Sql(@"
                DELETE FROM Catalogos
                WHERE Categoria = 'estado_palets' AND Descripcion = N'En revisión';");
        }
    }
}
