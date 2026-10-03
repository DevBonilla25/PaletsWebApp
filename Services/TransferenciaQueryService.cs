using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;

namespace PaletsWebApp.Services
{
    // Reúne los filtros que pueden enviar la web y la API.
    public class TransferenciaQuery
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string? SearchTerm { get; set; }
        public string? UserEnvia { get; set; }
        public string? UserRecibe { get; set; }
        public string? Estado { get; set; }
        public string? Palet { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // Contiene una página de transferencias y el total requerido por la vista web.
    public class TransferenciaPage
    {
        public List<View_Transferencia> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // Centraliza visibilidad, filtros, ordenamiento y paginación para web y API.
    public class TransferenciaQueryService
    {
        private readonly ApplicationDbContext _context;

        public TransferenciaQueryService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Construye una sola consulta SQL y obtiene únicamente la página solicitada.
        public async Task<TransferenciaPage> GetPageAsync(TransferenciaQuery request, bool includeTotalCount)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.TransferenciasView!.AsNoTracking();

            // Los administradores ven todo; los demás solo movimientos enviados o recibidos.
            if (!request.IsAdmin)
            {
                query = query.Where(x =>
                    x.ApplicationUserIdEnvia == request.UserId ||
                    x.ApplicationUserIdRecibe == request.UserId);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(x =>
                    x.UserEnviaFullName!.Contains(term) ||
                    x.UserRecibeFullName!.Contains(term) ||
                    x.DescEstado!.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(request.UserEnvia))
                query = query.Where(x => x.UserEnviaFullName!.Contains(request.UserEnvia));

            if (!string.IsNullOrWhiteSpace(request.UserRecibe))
                query = query.Where(x => x.UserRecibeFullName!.Contains(request.UserRecibe));

            if (!string.IsNullOrWhiteSpace(request.Estado) && request.Estado != "-1")
                query = query.Where(x => x.Estado == request.Estado);

            if (request.FechaDesde.HasValue)
                query = query.Where(x => x.FechaEnvio >= request.FechaDesde.Value);

            if (request.FechaHasta.HasValue)
                query = query.Where(x => x.FechaEnvio <= request.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(request.Palet))
            {
                query = query.Where(transferencia =>
                    _context.Detalles!.Any(detalle =>
                        detalle.IdTransferencia == transferencia.Id &&
                        _context.Palets!.Any(palet =>
                            palet.Id == detalle.IdPalet && palet.Descripcion == request.Palet)));
            }

            // Id descendente usa la clave indexada y evita el costoso ordenamiento por fecha.
            query = request.SortOrder switch
            {
                "name_desc" => query.OrderByDescending(x => x.UserEnviaFullName),
                "Date" => query.OrderBy(x => x.FechaEnvio),
                "date_desc" => query.OrderByDescending(x => x.FechaEnvio),
                _ => query.OrderByDescending(x => x.Id)
            };

            var totalCount = includeTotalCount ? await query.CountAsync() : 0;
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new TransferenciaPage
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
