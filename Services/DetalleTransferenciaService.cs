using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;

namespace PaletsWebApp.Services
{
    public sealed class DetalleTransferenciaService
    {
        private readonly ApplicationDbContext _context;

        public DetalleTransferenciaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetEstadoIdAsync(string descripcion)
        {
            var estadoId = await _context.Catalogos!
                .Where(x => x.Categoria == DetalleTransferenciaEstados.Categoria &&
                            x.Descripcion == descripcion)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync();

            if (estadoId == null)
            {
                throw new InvalidOperationException(
                    $"No está configurado el estado de detalle '{descripcion}'.");
            }

            return estadoId.Value.ToString();
        }

        public async Task<List<Detalle>> GetPendientesAsync(int transferenciaId)
        {
            var pendienteId = await GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);

            // El valor vacío mantiene compatibilidad hasta aplicar la migración que inicializa históricos.
            return await _context.Detalles!
                .Where(x => x.IdTransferencia == transferenciaId &&
                            (x.Estado == pendienteId || x.Estado == string.Empty))
                .ToListAsync();
        }

        public static void CambiarEstado(
            IEnumerable<Detalle> detalles,
            string estadoId,
            string? usuarioId,
            string? observaciones = null)
        {
            var fecha = DateTime.UtcNow;
            foreach (var detalle in detalles)
            {
                detalle.Estado = estadoId;
                detalle.FechaEstado = fecha;
                detalle.ApplicationUserIdResuelve = usuarioId;
                detalle.Observaciones = observaciones;
            }
        }

        public async Task RecalcularCabecerasAsync(IEnumerable<int> transferenciasIds)
        {
            var ids = transferenciasIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var estadosDetalle = await _context.Catalogos!
                .Where(x => x.Categoria == DetalleTransferenciaEstados.Categoria)
                .ToDictionaryAsync(x => x.Descripcion!, x => x.Id.ToString());
            var estadosTransferencia = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_transferencia")
                .ToDictionaryAsync(x => x.Descripcion!, x => x.Id.ToString());
            var transferencias = await _context.Transferencias!
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
            var detalles = await _context.Detalles!
                .Where(x => ids.Contains(x.IdTransferencia))
                .ToListAsync();

            foreach (var transferencia in transferencias)
            {
                var estados = detalles
                    .Where(x => x.IdTransferencia == transferencia.Id)
                    .Select(x => x.Estado)
                    .ToList();
                if (estados.Count == 0)
                {
                    continue;
                }

                if (estados.Any(x => x == estadosDetalle[DetalleTransferenciaEstados.Pendiente]))
                {
                    transferencia.Estado = transferencia.ApplicationUserIdRecibe == "Administradores"
                        ? estadosTransferencia["Por reclamar"]
                        : estadosTransferencia["Por recibir"];
                }
                else if (estados.All(x => x == estadosDetalle[DetalleTransferenciaEstados.Recibido]))
                {
                    transferencia.Estado = estadosTransferencia["Recibido"];
                    transferencia.FechaRecibo = DateTime.UtcNow;
                }
                else if (estados.All(x => x == estadosDetalle[DetalleTransferenciaEstados.Rechazado]))
                {
                    transferencia.Estado = estadosTransferencia["Rechazado"];
                    transferencia.FechaRechazo = DateTime.UtcNow;
                }
                else if (estados.All(x => x == estadosDetalle[DetalleTransferenciaEstados.Anulado]))
                {
                    transferencia.Estado = estadosTransferencia["Anulado"];
                    transferencia.FechaAnulado = DateTime.UtcNow;
                }
                else if (estados.All(x => x == estadosDetalle[DetalleTransferenciaEstados.Reclamado]))
                {
                    transferencia.Estado = estadosTransferencia["Reclamado"];
                    transferencia.FechaRecibo = DateTime.UtcNow;
                }
                else
                {
                    transferencia.Estado = estadosTransferencia["Procesado parcialmente"];
                }
            }
        }
    }
}
