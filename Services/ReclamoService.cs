using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;
using System.Data;

namespace PaletsWebApp.Services
{
    public sealed class ReclamoDesplazado
    {
        public int TransferenciaId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public List<string> Pallets { get; set; } = new();
    }

    public sealed class ResultadoAdjudicacionReclamo
    {
        public List<ReclamoDesplazado> ReclamosDesplazados { get; set; } = new();
    }

    public sealed class ReclamoService
    {
        private const string ReceptorAdministradores = "Administradores";
        private readonly ApplicationDbContext _context;
        private readonly DetalleTransferenciaService _detalleService;
        private readonly ILogger<ReclamoService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReclamoService(
            ApplicationDbContext context,
            DetalleTransferenciaService detalleService,
            ILogger<ReclamoService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _detalleService = detalleService;
            _logger = logger;
            _userManager = userManager;
        }

        // El primer reclamo aprobado gana cada pallet y cierra los reclamos competidores.
        public async Task<ResultadoAdjudicacionReclamo> AdjudicarAsync(
            int transferenciaId,
            string administradorId,
            string? observaciones)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var estadosTransferencia = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_transferencia")
                .ToDictionaryAsync(x => x.Descripcion!, x => x.Id.ToString());
            var estadoPendiente = await _detalleService.GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
            var estadoReclamadoDetalle = await _detalleService.GetEstadoIdAsync(DetalleTransferenciaEstados.Reclamado);
            var estadoRechazadoDetalle = await _detalleService.GetEstadoIdAsync(DetalleTransferenciaEstados.Rechazado);
            var estadoRetirado = await _detalleService.GetEstadoIdAsync(DetalleTransferenciaEstados.RetiradoPorCorreccion);
            var estadoReclamadoPalet = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_palets" && x.Descripcion == "Reclamado")
                .Select(x => x.Id.ToString())
                .SingleAsync();

            var reclamo = await _context.Transferencias!.SingleOrDefaultAsync(x => x.Id == transferenciaId);
            if (reclamo == null ||
                reclamo.ApplicationUserIdRecibe != ReceptorAdministradores ||
                reclamo.Estado != estadosTransferencia["Por reclamar"])
            {
                throw new InvalidOperationException("El reclamo ya no está pendiente de resolución.");
            }

            var detallesGanadores = await _context.Detalles!
                .Where(x => x.IdTransferencia == transferenciaId &&
                            (x.Estado == estadoPendiente || x.Estado == string.Empty))
                .ToListAsync();
            if (detallesGanadores.Count == 0)
            {
                throw new InvalidOperationException("El reclamo no tiene pallets pendientes.");
            }

            var idsPalets = detallesGanadores.Select(x => x.IdPalet).Distinct().ToList();
            var pallets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
            var nombresPallets = pallets.ToDictionary(x => x.Id, x => x.Descripcion ?? $"Pallet #{x.Id}");

            var detallesCompetidores = await (from detalle in _context.Detalles!
                                               join transferencia in _context.Transferencias!
                                                   on detalle.IdTransferencia equals transferencia.Id
                                               where detalle.IdTransferencia != transferenciaId &&
                                                     idsPalets.Contains(detalle.IdPalet) &&
                                                     (detalle.Estado == estadoPendiente || detalle.Estado == string.Empty) &&
                                                     transferencia.ApplicationUserIdRecibe == ReceptorAdministradores
                                               select detalle).ToListAsync();

            var detallesTransferencias = await (from detalle in _context.Detalles!
                                                 join transferencia in _context.Transferencias!
                                                     on detalle.IdTransferencia equals transferencia.Id
                                                 where detalle.IdTransferencia != transferenciaId &&
                                                       idsPalets.Contains(detalle.IdPalet) &&
                                                       (detalle.Estado == estadoPendiente || detalle.Estado == string.Empty) &&
                                                       transferencia.ApplicationUserIdRecibe != ReceptorAdministradores
                                                 select detalle).ToListAsync();

            foreach (var pallet in pallets)
            {
                pallet.Estado = estadoReclamadoPalet;
                pallet.ApplicationUserId = reclamo.ApplicationUserIdEnvia;
            }

            DetalleTransferenciaService.CambiarEstado(
                detallesGanadores, estadoReclamadoDetalle, administradorId, observaciones);
            DetalleTransferenciaService.CambiarEstado(
                detallesCompetidores, estadoRechazadoDetalle, administradorId,
                $"Pallet adjudicado a otro usuario mediante el reclamo #{transferenciaId}.");
            DetalleTransferenciaService.CambiarEstado(
                detallesTransferencias, estadoRetirado, administradorId,
                $"Custodia corregida mediante el reclamo #{transferenciaId}.");

            var transferenciasARecalcular = detallesGanadores
                .Concat(detallesCompetidores)
                .Concat(detallesTransferencias)
                .Select(x => x.IdTransferencia)
                .Distinct()
                .ToList();
            await _detalleService.RecalcularCabecerasAsync(transferenciasARecalcular);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var idsCompetidores = detallesCompetidores.Select(x => x.IdTransferencia).Distinct().ToList();
            var usuariosCompetidores = await _context.Transferencias!.AsNoTracking()
                .Where(x => idsCompetidores.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.ApplicationUserIdEnvia ?? string.Empty);

            var resultado = new ResultadoAdjudicacionReclamo
            {
                ReclamosDesplazados = detallesCompetidores
                    .GroupBy(x => x.IdTransferencia)
                    .Select(grupo => new ReclamoDesplazado
                    {
                        TransferenciaId = grupo.Key,
                        UsuarioId = usuariosCompetidores.GetValueOrDefault(grupo.Key, string.Empty),
                        Pallets = grupo.Select(x => nombresPallets[x.IdPalet]).ToList()
                    })
                    .ToList()
            };

            await NotificarDesplazadosAsync(resultado, reclamo.ApplicationUserIdEnvia);
            return resultado;
        }

        public async Task NotificarSupervisoresAsync(ApplicationUser reclamante, int reclamoId)
        {
            var supervisores = await _userManager.GetUsersInRoleAsync(WebsiteRoles.Supervisor!);
            foreach (var supervisor in supervisores)
            {
                try
                {
                    await Utils.SendNotification(
                        supervisor.FirebaseToken,
                        supervisor.Email ?? string.Empty,
                        $"{supervisor.Nombres} {supervisor.Apellidos}".Trim(),
                        "Nuevo reclamo de pallets",
                        $"{reclamante.Nombres} {reclamante.Apellidos} generó el reclamo #{reclamoId}. " +
                        "Revíselo para aceptar o rechazar.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo notificar al supervisor {SupervisorId} sobre el reclamo {ReclamoId}.",
                        supervisor.Id,
                        reclamoId);
                }
            }
        }

        public async Task NotificarResultadoAsync(
            int reclamoId,
            string usuarioProcesaId,
            bool aceptado)
        {
            var reclamo = await _context.Transferencias!.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == reclamoId);
            if (reclamo == null || string.IsNullOrWhiteSpace(reclamo.ApplicationUserIdEnvia))
                return;

            var idsUsuarios = new[] { reclamo.ApplicationUserIdEnvia, usuarioProcesaId };
            var usuarios = await _userManager.Users.AsNoTracking()
                .Where(x => idsUsuarios.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);
            if (!usuarios.TryGetValue(reclamo.ApplicationUserIdEnvia, out var reclamante))
                return;

            var nombreProcesa = usuarios.TryGetValue(usuarioProcesaId, out var usuarioProcesa)
                ? $"{usuarioProcesa.Nombres} {usuarioProcesa.Apellidos}".Trim()
                : "un supervisor";
            var resultado = aceptado ? "aceptado" : "rechazado";

            await Utils.SendNotification(
                reclamante.FirebaseToken,
                reclamante.Email ?? string.Empty,
                $"{reclamante.Nombres} {reclamante.Apellidos}".Trim(),
                $"Reclamo {resultado}",
                $"Tu reclamo #{reclamoId} fue {resultado} por {nombreProcesa}.");
        }

        private async Task NotificarDesplazadosAsync(
            ResultadoAdjudicacionReclamo resultado,
            string? ganadorId)
        {
            if (resultado.ReclamosDesplazados.Count == 0)
                return;

            var idsUsuarios = resultado.ReclamosDesplazados
                .Select(x => x.UsuarioId)
                .Append(ganadorId ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            var usuarios = await _userManager.Users.AsNoTracking()
                .Where(x => idsUsuarios.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);
            var nombreGanador = ganadorId != null && usuarios.TryGetValue(ganadorId, out var ganador)
                ? $"{ganador.Nombres} {ganador.Apellidos}".Trim()
                : "otro usuario";

            foreach (var desplazado in resultado.ReclamosDesplazados)
            {
                if (!usuarios.TryGetValue(desplazado.UsuarioId, out var usuario))
                    continue;

                try
                {
                    await Utils.SendNotification(
                        usuario.FirebaseToken,
                        usuario.Email ?? string.Empty,
                        $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                        "Reclamo resuelto por otro usuario",
                        $"{string.Join(", ", desplazado.Pallets)} fue asignado a {nombreGanador}.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo notificar al usuario {UsuarioId} sobre el reclamo desplazado {TransferenciaId}.",
                        desplazado.UsuarioId,
                        desplazado.TransferenciaId);
                }
            }
        }
    }
}
