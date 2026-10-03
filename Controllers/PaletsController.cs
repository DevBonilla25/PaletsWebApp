using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;
using PaletsWebApp.ViewModels;
using System.Diagnostics;

namespace PaletsWebApp.Controllers
{
    [Authorize]
    public class PaletsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public INotyfService _notification { get; }
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PaletsController(ApplicationDbContext context,
                                INotyfService notyfService,
                                UserManager<ApplicationUser> userManager,
                                RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _notification = notyfService;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [HttpGet]
        public async Task<IActionResult> Index(
            string sortOrder,
            string filterDescripcion,
            string sDescripcion,
            string filterUser,
            string sUser,
            string filterEstado,
            string sEstado,
            int? pageNumber)
        {

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (sDescripcion != null) pageNumber = 1;
            else sDescripcion = filterDescripcion;

            if (sUser != null) pageNumber = 1;
            else sUser = filterUser;

            
            if (sEstado != null) pageNumber = 1;
            else sEstado = filterEstado;

            ViewData["filterUser"] = sUser;
            ViewData["filterEstado"] = sEstado;
            ViewData["filterDescripcion"] = sDescripcion;

            var Palets = _context.PaletsView!.AsNoTracking();

            


            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if (!loggedInUserRole.Contains(WebsiteRoles.Admin) &&
                !loggedInUserRole.Contains(WebsiteRoles.Supervisor))
            {
                Palets = from s in Palets
                         where s.ApplicationUserId == loggedInUser!.Id
                         select s;
            }


            if (!String.IsNullOrEmpty(sDescripcion))
            {
                Palets = Palets.Where(s => s.Descripcion!.Contains(sDescripcion));
            }

            if (!String.IsNullOrEmpty(sUser))
            {
                Palets = Palets.Where(s => s.UserFullName!.Contains(sUser));
            }


            if (!String.IsNullOrEmpty(sEstado) && sEstado != "-1")
            {
                Palets = Palets.Where(s => s.Estado == sEstado);
            }


            var countsByStatus = await Palets
                .GroupBy(x => x.DescEstado)
                .Select(group => new { Estado = group.Key, Total = group.Count() })
                .ToListAsync();

            int CountStatus(string status) => countsByStatus
                .Where(x => string.Equals(x.Estado, status, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Total);

            ViewBag.ct_disponibles = CountStatus("disponible");
            ViewBag.ct_reclamados = CountStatus("reclamado");
            ViewBag.ct_en_transfer = CountStatus("en transferencia");
            ViewBag.ct_de_baja = CountStatus("dado de baja");



            switch (sortOrder)
            {
                case "name_desc":
                    Palets = Palets.OrderByDescending(s => s.UserFullName);
                    break;
                case "Date":
                    Palets = Palets.OrderBy(s => s.FechaCreacion);
                    break;
                case "date_desc":
                    Palets = Palets.OrderByDescending(s => s.FechaCreacion);
                    break;
                default:
                    Palets = Palets.OrderByDescending(s => s.Id);
                    break;
            }


            int pageSize = 5;
            return View(await PaginatedList<View_Palet>.CreateAsync(Palets, pageNumber ?? 1, pageSize));


        }


        [HttpGet]
        public IActionResult Create()
        {
            var vm = new PaletVM();

            var lstUsers = from du in _context.UsersView
                           where du.Activo == true
                           orderby du.Apellidos
                           select new SelectListItem
                           {
                               Value = du.Id,
                               Text = du.Apellidos + " " + du.Nombres + " (" + du.RolName + ")",
                               Selected = false
                           };

            vm.UserList = lstUsers.ToList();

            return View(vm);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaletVM vm)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(vm.Descripcion)) { 
                return View(vm); 
            }

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);

            if (loggedInUser == null)
            {
                return Unauthorized();
            }

            var defaultEstado = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

            var reg = new Palet();

            reg.Descripcion = vm.Descripcion;
            reg.Observaciones = vm.Observaciones;
            //reg.ApplicationUserId = defaultUser!.Id;
            reg.ApplicationUserId = vm.ApplicationUserId;
            reg.FechaCreacion = DateTime.Now;
            reg.Estado = defaultEstado.Id.ToString();


            await _context.Palets!.AddAsync(reg);
            await _context.SaveChangesAsync();


            var regUserDestino = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == vm.ApplicationUserId);
            if (regUserDestino != null)
            {
                await Utils.SendNotification(regUserDestino.FirebaseToken,
                                             regUserDestino.Email ?? string.Empty,
                                             regUserDestino.Nombres + " " + regUserDestino.Apellidos,
                                             "Se te ha asignado un pallet",
                                             "El usuario " + loggedInUser.Nombres + " " + loggedInUser.Apellidos + " te ha asignado el pallet '" + vm.Descripcion + "'");
            }


            _notification.Success("Palet creado exitosamente");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var estadosPalets = _context.Catalogos!.Where(x => x.Categoria == "estado_palets").ToList();

            var regPalet = await _context.Palets!.Where(x => x.Id == id).SingleAsync();

            var paletUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regPalet.ApplicationUserId);

            if (paletUser == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(paletUser);
            string rolUser = roles.FirstOrDefault()!;

            var vm = new PaletVM { 
                 Id = id,
                 Descripcion = regPalet.Descripcion ?? string.Empty,
                 ApplicationUserId = regPalet.ApplicationUserId,
                 ApplicationUserName = paletUser.Nombres + " " + paletUser.Apellidos,
                 RolUser = rolUser,
                 Estado = regPalet.Estado,
                 DescEstado = estadosPalets.Where(x => x.Id.ToString() == regPalet.Estado).SingleOrDefault()!.Descripcion,
                 Observaciones = regPalet.Observaciones,
            };

            var lstEstados = from de in estadosPalets
                             where de.Descripcion!.ToLower() != "en transferencia"
                             select new SelectListItem
                                {
                                    Value = de.Id.ToString(),
                                    Text = de.Descripcion,
                                    Selected = false
                                };

            vm.EstatusList = lstEstados.ToList();

            ViewBag.estadoDisponible = estadosPalets.Where(x => x.Descripcion!.ToLower() == "disponible").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoEnTransferencia = estadosPalets.Where(x => x.Descripcion!.ToLower() == "en transferencia").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoDadoBaja = estadosPalets.Where(x => x.Descripcion!.ToLower() == "dado de baja").SingleOrDefault()!.Id.ToString();
            ViewBag.loggedInUserId = loggedInUser!.Id;

            return View(vm);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(PaletVM vm)
        {

            var regPalet = await _context.Palets!.Where(x => x.Id == vm.Id).SingleAsync();


            if (!string.IsNullOrEmpty(vm.Estado) && vm.Estado != "-1" 
                && !string.IsNullOrEmpty(vm.Descripcion))
            { 
                regPalet.Estado = vm.Estado;
                regPalet.Descripcion = vm.Descripcion;
                regPalet.Observaciones = vm.Observaciones;

                await _context.SaveChangesAsync();

                _notification.Success("Pallet actualizado exitosamente");
                return RedirectToAction("Index");

            }
            


            var estadosPalets = _context.Catalogos!.Where(x => x.Categoria == "estado_palets").ToList();

            var paletUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regPalet.ApplicationUserId);

            if (paletUser == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(paletUser);
            string rolUser = roles.FirstOrDefault()!;

            string newDescEstado = "";
            if (!string.IsNullOrEmpty(vm.Estado) && vm.Estado != "-1")
            {
                newDescEstado = estadosPalets.FirstOrDefault(x => x.Id.ToString() == vm.Estado)?.Descripcion ?? string.Empty;
            }

            var vm2 = new PaletVM
            {
                Id = vm.Id,
                Descripcion = vm.Descripcion,
                ApplicationUserId = regPalet.ApplicationUserId,
                ApplicationUserName = paletUser.Nombres + " " + paletUser.Apellidos,
                RolUser = rolUser,
                Estado = vm.Estado,
                DescEstado = newDescEstado,
                Observaciones = vm.Observaciones
               
            };


            var lstEstados = from de in estadosPalets
                             where de.Descripcion!.ToLower() != "en transferencia"
                             select new SelectListItem
                             {
                                 Value = de.Id.ToString(),
                                 Text = de.Descripcion,
                                 Selected = de.Id.ToString() == vm.Estado ? true : false
                             };

            vm2.EstatusList = lstEstados.ToList();

            return View(vm2);



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




