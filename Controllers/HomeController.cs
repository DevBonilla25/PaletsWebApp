using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaletsWebApp.Models;
using PaletsWebApp.Data;
using PaletsWebApp.ViewModels;
using PaletsWebApp.Utilites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace PaletsWebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await BuildDashboardAsync();
            return dashboard == null ? Unauthorized() : View(dashboard);
        }

        private async Task<DashboardVM?> BuildDashboardAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            var all = roles.Contains(WebsiteRoles.Admin) || roles.Contains(WebsiteRoles.Supervisor);
            var palets = _context.PaletsView!.AsNoTracking();
            var transfers = _context.TransferenciasView!.AsNoTracking();
            if (!all)
            {
                palets = palets.Where(x => x.ApplicationUserId == user.Id);
                transfers = transfers.Where(x => x.ApplicationUserIdEnvia == user.Id || x.ApplicationUserIdRecibe == user.Id);
            }
            return await CreateDashboardAsync(user, palets, transfers);
        }

        private static async Task<DashboardVM> CreateDashboardAsync(ApplicationUser user, IQueryable<View_Palet> palets, IQueryable<View_Transferencia> transfers)
        {
            var vm = new DashboardVM { UserName = user.Nombres ?? user.UserName ?? "Usuario" };
            vm.TotalPalets = await palets.CountAsync();
            vm.PaletsDisponibles = await palets.CountAsync(x => x.DescEstado == "Disponible");
            vm.PaletsEnTransferencia = await palets.CountAsync(x => x.DescEstado == "En transferencia");
            vm.TransferenciasPendientes = await transfers.CountAsync(x => x.DescEstado == "Por recibir" || x.DescEstado == "Por reclamar");
            // Estas vistas no permiten traducir de forma fiable GroupBy + coalescencia
            // a SQL. Traemos solamente el estado y construimos los totales en memoria.
            var estadosPalets = await palets.Select(x => x.DescEstado).ToListAsync();
            vm.PaletsPorEstado = estadosPalets
                .GroupBy(x => string.IsNullOrWhiteSpace(x) ? "Sin estado" : x)
                .ToDictionary(x => x.Key!, x => x.Count());
            var estadosTransferencias = await transfers.Select(x => x.DescEstado).ToListAsync();
            vm.TransferenciasPorEstado = estadosTransferencias
                .GroupBy(x => string.IsNullOrWhiteSpace(x) ? "Sin estado" : x)
                .ToDictionary(x => x.Key!, x => x.Count());
            // El Id sigue el orden de creación y evita ordenar toda la vista por una fecha no indexada.
            vm.TransferenciasRecientes = await transfers
                .OrderByDescending(x => x.Id)
                .Take(6)
                .ToListAsync();
            return vm;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
