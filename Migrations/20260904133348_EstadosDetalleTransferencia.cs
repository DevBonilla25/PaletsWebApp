using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaletsWebApp.Migrations
{
    public partial class EstadosDetalleTransferencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserIdResuelve",
                table: "Detalles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Detalles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEstado",
                table: "Detalles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Detalles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalles_IdPalet_Estado",
                table: "Detalles",
                columns: new[] { "IdPalet", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Detalles_IdTransferencia_Estado",
                table: "Detalles",
                columns: new[] { "IdTransferencia", "Estado" });

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Pendiente')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'Pendiente');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Recibido')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'Recibido');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Rechazado')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'Rechazado');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Anulado')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'Anulado');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'En revisión')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'En revisión');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Retirado por corrección')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'Retirado por corrección');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Reclamado')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_detalle_transferencia', 0, 0, N'Reclamado');
                IF NOT EXISTS (SELECT 1 FROM Catalogos WHERE Categoria = 'estado_transferencia' AND Descripcion = N'Procesado parcialmente')
                    INSERT INTO Catalogos (Categoria, Minimo, Maximo, Descripcion) VALUES ('estado_transferencia', 0, 0, N'Procesado parcialmente');

                DECLARE @PendienteId nvarchar(20) = CAST((
                    SELECT TOP (1) Id FROM Catalogos
                    WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Pendiente'
                ) AS nvarchar(20));
                DECLARE @RecibidoId nvarchar(20) = CAST((
                    SELECT TOP (1) Id FROM Catalogos
                    WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Recibido'
                ) AS nvarchar(20));
                DECLARE @RechazadoId nvarchar(20) = CAST((
                    SELECT TOP (1) Id FROM Catalogos
                    WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Rechazado'
                ) AS nvarchar(20));
                DECLARE @AnuladoId nvarchar(20) = CAST((
                    SELECT TOP (1) Id FROM Catalogos
                    WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Anulado'
                ) AS nvarchar(20));
                DECLARE @ReclamadoId nvarchar(20) = CAST((
                    SELECT TOP (1) Id FROM Catalogos
                    WHERE Categoria = 'estado_detalle_transferencia' AND Descripcion = N'Reclamado'
                ) AS nvarchar(20));

                UPDATE detalle
                SET detalle.Estado = CASE LOWER(estadoTransferencia.Descripcion)
                        WHEN N'recibido' THEN @RecibidoId
                        WHEN N'rechazado' THEN @RechazadoId
                        WHEN N'anulado' THEN @AnuladoId
                        WHEN N'reclamado' THEN @ReclamadoId
                        ELSE @PendienteId
                    END,
                    detalle.FechaEstado = CASE LOWER(estadoTransferencia.Descripcion)
                        WHEN N'recibido' THEN COALESCE(NULLIF(transferencia.FechaRecibo, '0001-01-01'), transferencia.FechaEnvio)
                        WHEN N'rechazado' THEN COALESCE(NULLIF(transferencia.FechaRechazo, '0001-01-01'), transferencia.FechaEnvio)
                        WHEN N'anulado' THEN COALESCE(NULLIF(transferencia.FechaAnulado, '0001-01-01'), transferencia.FechaEnvio)
                        WHEN N'reclamado' THEN COALESCE(NULLIF(transferencia.FechaRecibo, '0001-01-01'), transferencia.FechaEnvio)
                        ELSE transferencia.FechaEnvio
                    END
                FROM Detalles detalle
                INNER JOIN Transferencias transferencia ON transferencia.Id = detalle.IdTransferencia
                LEFT JOIN Catalogos estadoTransferencia
                    ON estadoTransferencia.Categoria = 'estado_transferencia'
                   AND CAST(estadoTransferencia.Id AS nvarchar(20)) = transferencia.Estado
                WHERE detalle.Estado = ''; ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Detalles_IdPalet_Estado",
                table: "Detalles");

            migrationBuilder.DropIndex(
                name: "IX_Detalles_IdTransferencia_Estado",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "ApplicationUserIdResuelve",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "FechaEstado",
                table: "Detalles");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Detalles");

            migrationBuilder.Sql(@"
                DELETE FROM Catalogos
                WHERE Categoria = 'estado_detalle_transferencia'
                   OR (Categoria = 'estado_transferencia' AND Descripcion = N'Procesado parcialmente');");
        }
    }
}
