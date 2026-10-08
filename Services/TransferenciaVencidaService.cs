using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;
using System.Data;

namespace PaletsWebApp.Services
{
    public sealed class TransferenciaVencidaService
    {
        private readonly ApplicationDbContext _context;
        private readonly DetalleTransferenciaService _detalleService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TransferenciaVencidaService> _logger;

        public TransferenciaVencidaService(
            ApplicationDbContext context,
            DetalleTransferenciaService detalleService,
            UserManager<ApplicationUser> userManager,
            ILogger<TransferenciaVencidaService> logger)
        {
            _context = context;
            _detalleService = detalleService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task ProcesarAsync(CancellationToken cancellationToken)
        {
            var estadoPorRecibir = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_transferencia" && x.Descripcion == "Por recibir")
                .Select(x => x.Id.ToString())
                .SingleAsync(cancellationToken);
            var ahora = DateTime.UtcNow;

            var transferenciasIds = await _context.Transferencias!
                .AsNoTracking()
                .Where(x => x.Estado == estadoPorRecibir &&
                            x.FechaLimiteAceptacion.HasValue &&
                            x.FechaLimiteAceptacion <= ahora &&
                            x.ApplicationUserIdRecibe != null)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (var transferenciaId in transferenciasIds)
            {
                try
                {
                    var procesada = await ProcesarUnaAsync(
                        transferenciaId, estadoPorRecibir, ahora, cancellationToken);
                    if (procesada != null)
                        await NotificarAsync(procesada, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "No se pudo aceptar automáticamente la transferencia {TransferenciaId}.",
                        transferenciaId);
                }
            }
        }

        private async Task<Transferencia?> ProcesarUnaAsync(
            int transferenciaId,
            string estadoPorRecibir,
            DateTime ahora,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var transferencia = await _context.Transferencias!
                .SingleOrDefaultAsync(x => x.Id == transferenciaId, cancellationToken);

            if (transferencia == null ||
                transferencia.Estado != estadoPorRecibir ||
                !transferencia.FechaLimiteAceptacion.HasValue ||
                transferencia.FechaLimiteAceptacion > ahora ||
                transferencia.ApplicationUserIdRecibe == null)
            {
                return null;
            }

            var detalles = await _detalleService.GetPendientesAsync(transferenciaId);
            if (detalles.Count == 0)
            {
                await _detalleService.RecalcularCabecerasAsync(new[] { transferenciaId });
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            var idsPalets = detalles.Select(x => x.IdPalet).ToList();
            var pallets = await _context.Palets!
                .Where(x => idsPalets.Contains(x.Id))
                .ToListAsync(cancellationToken);
            var estadoDisponible = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_palets" && x.Descripcion == "Disponible")
                .Select(x => x.Id.ToString())
                .SingleAsync(cancellationToken);
            var estadoRecibido = await _detalleService
                .GetEstadoIdAsync(DetalleTransferenciaEstados.Recibido);

            foreach (var pallet in pallets)
            {
                pallet.Estado = estadoDisponible;
                pallet.ApplicationUserId = transferencia.ApplicationUserIdRecibe;
            }
            DetalleTransferenciaService.CambiarEstado(
                detalles,
                estadoRecibido,
                "Sistema",
                "Aceptación automática por vencimiento del plazo de 48 horas.");
            await _detalleService.RecalcularCabecerasAsync(new[] { transferenciaId });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return transferencia;
        }

        private async Task NotificarAsync(
            Transferencia transferencia,
            CancellationToken cancellationToken)
        {
            var ids = new[]
            {
                transferencia.ApplicationUserIdEnvia,
                transferencia.ApplicationUserIdRecibe
            }.Where(x => x != null).Cast<string>().Distinct().ToList();
            var usuarios = await _userManager.Users
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            foreach (var usuario in usuarios)
            {
                try
                {
                    await Utils.SendNotification(
                        usuario.FirebaseToken,
                        usuario.Email ?? string.Empty,
                        $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                        "Transferencia aceptada automáticamente",
                        $"La transferencia {transferencia.CodigoInterno} fue aceptada al vencer el plazo de 48 horas.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo notificar la aceptación automática al usuario {UsuarioId}.",
                        usuario.Id);
                }
            }
        }
    }

    public sealed class TransferenciasVencidasWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TransferenciasVencidasWorker> _logger;

        public TransferenciasVencidasWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<TransferenciasVencidasWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Procesa lo vencido al iniciar, por si el hosting estuvo suspendido
            // o se reinició durante la ejecución programada.
            await ProcesarVencidasAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TiempoHastaMedianocheEcuador(), stoppingToken);
                await ProcesarVencidasAsync(stoppingToken);

            }
        }

        private async Task ProcesarVencidasAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TransferenciaVencidaService>();
                await service.ProcesarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // El cierre de la aplicación cancela normalmente el proceso.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar transferencias vencidas.");
            }
        }

        private static TimeSpan TiempoHastaMedianocheEcuador()
        {
            var zonaEcuador = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var ahoraUtc = DateTime.UtcNow;
            var ahoraEcuador = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, zonaEcuador);
            var proximaMedianoche = ahoraEcuador.Date.AddDays(1);
            var proximaMedianocheUtc = TimeZoneInfo.ConvertTimeToUtc(proximaMedianoche, zonaEcuador);
            return proximaMedianocheUtc - ahoraUtc;
        }
    }
}
