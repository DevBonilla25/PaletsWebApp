using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaletsWebApp.Migrations
{
    public partial class InitDetalle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Detalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPalet = table.Column<int>(type: "int", nullable: false),
                    IdTransferencia = table.Column<int>(type: "int", nullable: false),
                    TransferenciaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detalle_Transferencias_TransferenciaId",
                        column: x => x.TransferenciaId,
                        principalTable: "Transferencias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_TransferenciaId",
                table: "Detalle",
                column: "TransferenciaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detalle");
        }
    }
}
