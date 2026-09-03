using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaletsWebApp.Migrations
{
    public partial class InitDetalle2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Transferencias_TransferenciaId",
                table: "Detalle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalle",
                table: "Detalle");

            migrationBuilder.RenameTable(
                name: "Detalle",
                newName: "Detalles");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_TransferenciaId",
                table: "Detalles",
                newName: "IX_Detalles_TransferenciaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalles",
                table: "Detalles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalles_Transferencias_TransferenciaId",
                table: "Detalles",
                column: "TransferenciaId",
                principalTable: "Transferencias",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalles_Transferencias_TransferenciaId",
                table: "Detalles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalles",
                table: "Detalles");

            migrationBuilder.RenameTable(
                name: "Detalles",
                newName: "Detalle");

            migrationBuilder.RenameIndex(
                name: "IX_Detalles_TransferenciaId",
                table: "Detalle",
                newName: "IX_Detalle_TransferenciaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalle",
                table: "Detalle",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Transferencias_TransferenciaId",
                table: "Detalle",
                column: "TransferenciaId",
                principalTable: "Transferencias",
                principalColumn: "Id");
        }
    }
}
