using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaletsWebApp.Migrations
{
    public partial class UpdateTransferRechazado : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalles_Transferencias_TransferenciaId",
                table: "Detalles");

            migrationBuilder.DropIndex(
                name: "IX_Detalles_TransferenciaId",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "TransferenciaId",
                table: "Detalles");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRechazo",
                table: "Transferencias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaRechazo",
                table: "Transferencias");

            migrationBuilder.AddColumn<int>(
                name: "TransferenciaId",
                table: "Detalles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalles_TransferenciaId",
                table: "Detalles",
                column: "TransferenciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalles_Transferencias_TransferenciaId",
                table: "Detalles",
                column: "TransferenciaId",
                principalTable: "Transferencias",
                principalColumn: "Id");
        }
    }
}
