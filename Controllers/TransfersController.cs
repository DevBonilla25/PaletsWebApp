using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;
using PaletsWebApp.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using X.PagedList;

namespace PaletsWebApp.Controllers
{
    [Authorize]
    public class TransfersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public INotyfService _notification { get; }
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        List<Catalogo> listEstadosTrans;

        public TransfersController(ApplicationDbContext context,
                                INotyfService notyfService,
                                IWebHostEnvironment webHostEnvironment,
                                UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notification = notyfService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;

            listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();

        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string sortOrder,
            string filterUserEnvia,
            string sUserEnvia,
            string filterUserRecibe,
            string sUserRecibe,
            string filterDate1,
            string sDate1,
            string filterDate2,
            string sDate2,
            string filterEstado,
            string sEstado,
            string sPalet,
            int? pageNumber)
        {

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (sUserEnvia != null) pageNumber = 1;
            else  sUserEnvia = filterUserEnvia;
            
            if (sUserRecibe != null) pageNumber = 1;
            else sUserRecibe = filterUserRecibe;

            if (sDate1 != null) pageNumber = 1;
            else sDate1 = filterDate1;
            
            if (sDate2 != null) pageNumber = 1;
            else sDate2 = filterDate2;

            if (sEstado != null) pageNumber = 1;
            else sEstado = filterEstado;
            
            ViewData["filterUserEnvia"] = sUserEnvia;
            ViewData["filterUserRecibe"] = sUserRecibe;
            ViewData["filterDate1"] = sDate1;
            ViewData["filterDate2"] = sDate2;
            ViewData["filterEstado"] = sEstado;
            ViewData["filterPalet"] = sPalet;



            var Transferencias = _context.TransferenciasView!.AsNoTracking();

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);

            // Determinar si el usuario puede reclamar palets
            ViewBag.CanReclaimPalets = !loggedInUserRole.Contains(WebsiteRoles.Cliente);

            if (loggedInUserRole[0] != WebsiteRoles.Admin)
            {
                //RefactorizaciÃ³n para legibilidad: Usa IQueryable en lugar de from para mejorar la claridad
                Transferencias = Transferencias.Where(s =>
                    s.ApplicationUserIdEnvia == loggedInUser!.Id ||
                    s.ApplicationUserIdRecibe == loggedInUser!.Id);

            }



            if (!String.IsNullOrEmpty(sUserEnvia))
            {
                Transferencias = Transferencias.Where(s => s.UserEnviaFullName!.Contains(sUserEnvia));
            }

            if (!String.IsNullOrEmpty(sUserRecibe))
            {
                Transferencias = Transferencias.Where(s => s.UserRecibeFullName!.Contains(sUserRecibe));
            }

            if (!String.IsNullOrEmpty(sEstado) && sEstado != "-1")
            {
                Transferencias = Transferencias.Where(s => s.Estado == sEstado);
            }


            if (!String.IsNullOrEmpty(sDate1))
            {
                var cultureInfo = new CultureInfo("es-ES");
                var dateFrom = DateTime.Parse(sDate1, cultureInfo);
                Transferencias = Transferencias.Where(s => s.FechaEnvio >= dateFrom);
            }

            if (!String.IsNullOrEmpty(sDate2))
            {
                var cultureInfo = new CultureInfo("es-ES");
                var dateTo = DateTime.Parse(sDate2, cultureInfo).AddDays(1).AddSeconds(-1);
                Transferencias = Transferencias.Where(s => s.FechaEnvio <= dateTo);
            }


            if (!String.IsNullOrEmpty(sPalet))
            {

                Transferencias = Transferencias.Where(transferencia =>
                    _context.Detalles!.Any(detalle =>
                        detalle.IdTransferencia == transferencia.Id &&
                        _context.Palets!.Any(palet =>
                            palet.Id == detalle.IdPalet && palet.Descripcion == sPalet)));


            }



            switch (sortOrder)
            {
                case "name_desc":
                    Transferencias = Transferencias.OrderByDescending(s => s.UserEnviaFullName);
                    break;
                case "Date":
                    Transferencias = Transferencias.OrderBy(s => s.FechaEnvio);
                    break;
                case "date_desc":
                    Transferencias = Transferencias.OrderByDescending(s => s.FechaEnvio);
                    break;
                default:
                    Transferencias = Transferencias.OrderByDescending(s => s.Id);
                    break;
            }

            ViewBag.estadoRecibido = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoReclamado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "reclamado").SingleOrDefault()?.Id.ToString() ?? string.Empty;
            ViewBag.estadoRechazado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoAnulado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();


       

            int pageSize = 5;
            var paginatedTransfers = await PaginatedList<View_Transferencia>
                .CreateAsync(Transferencias, pageNumber ?? 1, pageSize);

            var transferIds = paginatedTransfers.Select(x => x.Id).ToList();
            var palletsByTransfer = await (from detalle in _context.Detalles!.AsNoTracking()
                                           join palet in _context.Palets!.AsNoTracking()
                                               on detalle.IdPalet equals palet.Id
                                           where transferIds.Contains(detalle.IdTransferencia)
                                           select new
                                           {
                                               detalle.IdTransferencia,
                                               palet.Descripcion
                                           })
                .ToListAsync();

            var palletDescriptions = palletsByTransfer
                .GroupBy(x => x.IdTransferencia)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(", ", group.Select(x => x.Descripcion).Where(x => !string.IsNullOrWhiteSpace(x))));

            foreach (var transfer in paginatedTransfers)
            {
                if (palletDescriptions.TryGetValue(transfer.Id, out var descriptions))
                {
                    transfer.CodigoInterno += " Pallets: " + descriptions;
                }
            }

            return View(paginatedTransfers);


        }


        //[HttpGet]
        //public async Task<IActionResult> Index_old(int? page)
        //{
        //    var listxxx = _context.TransferenciasView;

        //    var listyyy = _context.PaletsView;


        //    var listOfTransfers = new List<Transferencia>();

        //    var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
        //    var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
        //    if (loggedInUserRole[0] == WebsiteRoles.Admin)
        //    {
        //        listOfTransfers = await _context.Transferencias!.ToListAsync();
        //    }
        //    else
        //    {
        //        listOfTransfers = await _context.Transferencias!.
        //            Where(x => x.ApplicationUserIdEnvia == loggedInUser!.Id ||
        //                       x.ApplicationUserIdRecibe == loggedInUser!.Id).ToListAsync();
        //    }

        //    var qry = from tr in listOfTransfers
        //              join uenv in _userManager.Users on tr.ApplicationUserIdEnvia equals uenv.Id
        //              join urec in _userManager.Users on tr.ApplicationUserIdRecibe equals urec.Id
        //              select new TransferenciaVM
        //              {
        //                  Id = tr.Id,
        //                  CodigoInterno = tr.CodigoInterno,
        //                  FechaEnvio = tr.FechaEnvio,
        //                  FechaRecibo = tr.FechaRecibo,
        //                  FechaRechazo = tr.FechaRechazo,
        //                  IdUserEnvia = tr.ApplicationUserIdEnvia,
        //                  NombreUserEnvia = uenv.Nombres + " " + uenv.Apellidos,
        //                  IdUserRecibe = tr.ApplicationUserIdRecibe,
        //                  NombreUserRecibe = urec.Nombres + " " + urec.Apellidos,
        //                  Estado = listEstadosTrans.Where(y => y.Id.ToString() == tr.Estado).SingleOrDefault()!.Descripcion,

        //              };


        //    var qry2 = qry.ToList();

        //    int pageSize = 5;
        //    int pageNumber = (page ?? 1);

        //    var retList = await qry2.OrderByDescending(x => x.Id).ToPagedListAsync(pageNumber, pageSize);


        //    var vm = new TransferenciaMasterVM
        //    {
        //        Lista = retList,
        //        Filtro = new FilterTransVM { 
        //            EnviaDesde = "29/01/2023",
        //            UsuarioEnvia ="yoyo", 
        //            UsuarioRecibe="elel",
        //        }
        //    };


        //    ViewBag.estadoRecibido = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
        //    ViewBag.estadoRechazado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();

        //    return View(vm);
        //}

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // ObtÃ©n los IDs de los estados "disponible" y "reclamados"
            var estadosIds = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_palets" &&
                            (x.Descripcion!.ToLower() == "disponible" || x.Descripcion!.ToLower() == "reclamado"))
                .Select(x => x.Id.ToString())
                .ToListAsync();

            var loggedInUser = _userManager.Users.FirstOrDefault(x => x.UserName == User.Identity!.Name);
            if (loggedInUser == null)
            {
                return Unauthorized();
            }

            var regVM = new TransferenciaVM();

            // Filtra los pallets que tienen el estado "disponible" o "reclamados"
            var listOfPalets = await _context.Palets!.Include(x => x.ApplicationUser)
                .Where(x => x.ApplicationUser!.Id == loggedInUser!.Id)
                .Where(x => x.Estado != null && estadosIds.Contains(x.Estado))
                .ToListAsync();

            var paletsVM = from dr in listOfPalets
                           select new PaletVM
                           {
                               Id = dr.Id,
                               Descripcion = dr.Descripcion ?? string.Empty,
                               IsSelected = false,
                               Estado = dr.Estado,
                           };

            regVM.Palets = paletsVM.ToList();
            regVM.JsonPalets = JsonSerializer.Serialize(regVM.Palets);

            var lstUsers = from du in _userManager.Users
                           where du.Id != loggedInUser!.Id
                           select new SelectListItem
                           {
                               Value = du.Id,
                               Text = du.Nombres + " " + du.Apellidos,
                               Selected = false
                           };

            regVM.UserList = lstUsers.ToList();

            ViewBag.PalletRequired = false;

            return View(regVM);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferenciaVM vm)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);

            string cadIdUser = vm.IdUserRecibe == null ? "" : vm.IdUserRecibe;
            var ct = 0;
            if (vm.UqChecked != null)
                ct = vm.UqChecked!.Count();


            ViewBag.PalletRequired = false;

            if (cadIdUser == "" || ct == 0)
            {

                var list_paletsVM = JsonSerializer.Deserialize<List<PaletVM>>(vm.JsonPalets ?? "[]") ?? new List<PaletVM>();

                if (ct > 0)
                {
                    foreach (var itm in list_paletsVM)
                    {
                        if (vm.UqChecked!.Contains(itm.Id.ToString()))
                        {
                            itm.IsSelected = true;
                        }
                    }

                }

                vm.Palets = list_paletsVM;

                ViewBag.PalletRequired = list_paletsVM.Where(x => x.IsSelected == true).Count() == 0;


                var lstUsers = from du in _userManager.Users
                               where du.Id != loggedInUser!.Id
                               select new SelectListItem
                               {
                                   Value = du.Id,
                                   Text = du.Nombres + " " + du.Apellidos,
                                   Selected = du.Id == vm.IdUserRecibe ? true : false
                               };

                vm.UserList = lstUsers.ToList();


                return View(vm);
            }

            //get default values

            var defaultEstado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por recibir").SingleOrDefault();

            var reg = new Transferencia();

            //Estas fechas obtienes la hora del sistema operativo, por lo que en produccion toma la hora del servidor externo y no es el mismo del Ecuador
            //reg.CodigoInterno = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            //reg.FechaEnvio = DateTime.Now;

            // Cambiar DateTime.Now por DateTime.UtcNow para Ecuador
            reg.CodigoInterno = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
            reg.FechaEnvio = DateTime.UtcNow; // Guardar en UTC
            reg.ApplicationUserIdEnvia = loggedInUser!.Id;
            reg.ApplicationUserIdRecibe = vm.IdUserRecibe;
            reg.Estado = defaultEstado!.Id.ToString();


            await _context.Transferencias!.AddAsync(reg);

            await _context.SaveChangesAsync();

            var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();

            foreach (var pal in vm.UqChecked!)
            {
                var detalle = new Detalle
                {
                    IdPalet = int.Parse(pal),
                    IdTransferencia = reg.Id
                };
                await _context.Detalles!.AddAsync(detalle);

                var regPalet = await _context.Palets!.Where(x => x.Id == int.Parse(pal)).SingleAsync();
                regPalet.Estado = newEstadoPalet.Id.ToString();

            }

            await _context.SaveChangesAsync();

            var regUserDestino = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == vm.IdUserRecibe);
            if (regUserDestino != null)
            {
                await Utils.SendNotification(regUserDestino.FirebaseToken,
                                             regUserDestino.Email ?? string.Empty,
                                             regUserDestino.Nombres + " " + regUserDestino.Apellidos,
                                             "Has recibido una transferencia ",
                                             "El usuario " + loggedInUser.Nombres + " " + loggedInUser.Apellidos + " te ha realizado la transferencia con codigo '" + reg.CodigoInterno + "'");
            }



            _notification.Success("Transferencia creada exitosamente");
            return RedirectToAction("Index");
        }



        [HttpGet]
        public async Task<IActionResult> Reclamar(
            string sDescripcion, 
            string selectedPaletsJson)
        {
            var loggedInUser = _userManager.Users.FirstOrDefault(x => x.UserName == User.Identity!.Name);
            var regVM = new TransferenciaVM();

            // Inicializar la lista de pallets seleccionados
            List<PaletVM> selectedPalets = new();

            if (!string.IsNullOrEmpty(selectedPaletsJson))
            {
                // Deserializar pallets seleccionados previamente
                selectedPalets = JsonSerializer.Deserialize<List<PaletVM>>(selectedPaletsJson) ?? new List<PaletVM>();
            }

            // Si no hay filtro, no cargues pallets adicionales
            if (!string.IsNullOrWhiteSpace(sDescripcion))
            {
                // Aplicar filtro directamente en la consulta
                var query = _context.Palets!.Include(x => x.ApplicationUser)
                    .Where(x => x.ApplicationUser!.Id != loggedInUser!.Id &&
                                x.Descripcion != null && EF.Functions.Like(x.Descripcion, $"%{sDescripcion}%"));

                var filteredPalets = await query.ToListAsync();

                // Convertir a ViewModel
                var paletsVM = filteredPalets.Select(dr => new PaletVM
                {
                    Id = dr.Id,
                    Descripcion = dr.Descripcion ?? string.Empty,
                    IsSelected = selectedPalets.Any(p => p.Id == dr.Id), // Marcar como seleccionado si ya estÃ¡ en la lista
                    Estado = dr.Estado,
                }).ToList();

                // Combinar con los seleccionados previamente
                selectedPalets.AddRange(paletsVM.Where(p => !selectedPalets.Any(sp => sp.Id == p.Id)));
            }

            regVM.Palets = selectedPalets;
            regVM.JsonPalets = JsonSerializer.Serialize(selectedPalets);

            ViewBag.PalletRequired = false;

            return View(regVM);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reclamar(TransferenciaVM vm)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);

            var ct = 0;
            if (vm.UqChecked != null)
                ct = vm.UqChecked!.Count();

            ViewBag.PalletRequired = false;

            // Validar que al menos un pallet estÃ© seleccionado
            if (ct == 0)
            {
                var list_paletsVM = JsonSerializer.Deserialize<List<PaletVM>>(vm.JsonPalets ?? "[]") ?? new List<PaletVM>();

                if (ct > 0)
                {
                    foreach (var itm in list_paletsVM)
                    {
                        if (vm.UqChecked!.Contains(itm.Id.ToString()))
                        {
                            itm.IsSelected = true;
                        }
                    }
                }

                vm.Palets = list_paletsVM;

                ViewBag.PalletRequired = list_paletsVM.Where(x => x.IsSelected == true).Count() == 0;

                return View(vm);
            }

            // Obtener el estado "por reclamar" para la transferencia
            var estadoPorReclamar = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_transferencia" && x.Descripcion!.ToLower() == "por reclamar")
                .SingleOrDefaultAsync();

            if (estadoPorReclamar == null)
            {
                ModelState.AddModelError("", "No se pudo encontrar el estado 'por reclamar' en el sistema.");
                return View(vm);
            }

            // Crear el registro de transferencia para el reclamo
            var reg = new Transferencia
            {
                // Cambiar DateTime.Now por DateTime.UtcNow para Ecuador
                CodigoInterno = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss"),
                FechaEnvio = DateTime.UtcNow, // Guardar en UTC
                ApplicationUserIdEnvia = loggedInUser!.Id,
                ApplicationUserIdRecibe = "Administradores", // Sin asignar a ningÃºn administrador especÃ­fico
                Estado = estadoPorReclamar.Id.ToString()
            };

            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();

            // Cambiar el estado de los pallets seleccionados a "en reclamo"
            var estadoEnReclamo = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en reclamo")
                .SingleOrDefaultAsync();

            if (estadoEnReclamo == null)
            {
                ModelState.AddModelError("", "No se pudo encontrar el estado 'en reclamo' en el sistema.");
                return View(vm);
            }

            foreach (var pal in vm.UqChecked!)
            {
                var detalle = new Detalle
                {
                    IdPalet = int.Parse(pal),
                    IdTransferencia = reg.Id
                };
                await _context.Detalles!.AddAsync(detalle);

                var regPalet = await _context.Palets!.Where(x => x.Id == int.Parse(pal)).SingleAsync();
                regPalet.Estado = estadoEnReclamo.Id.ToString();
            }

            await _context.SaveChangesAsync();

            // Notificar a los administradores sobre el reclamo
            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrador");
            foreach (var admin in adminUsers)
            {
                await Utils.SendNotification(
                    admin.FirebaseToken,
                    admin.Email,
                    admin.Nombres + " " + admin.Apellidos,
                    "Nuevo reclamo de pallets",
                    "El usuario " + loggedInUser.Nombres + " " + loggedInUser.Apellidos +
                    " ha generado un reclamo de pallets. Revise el reclamo para tomar una acciÃ³n."
                );
            }

            _notification.Success("Reclamo creado exitosamente");
            return RedirectToAction("Index");
        }



        // Aqui procesamos si es Transferwencia o Reclamo
        private async Task<(bool Success, string Tipo)> procesarTransfer(TransferenciaVM vm, string operacion)
        {
            bool ret = false;
            string tipo = "transferencia";

            var reg = await _context.Transferencias!.Where(x => x.Id == vm.Id).SingleAsync();
            reg!.Observaciones = vm.Observaciones;

            if (operacion == "aceptar")
            {
                if (reg.Estado == listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault()!.Id.ToString())
                {
                    // Es un reclamo
                    reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "reclamado").SingleOrDefault()!.Id.ToString();
                    reg.FechaRecibo = DateTime.UtcNow;
                    tipo = "reclamo";

                    var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();
                    var newEstadoPalet = await _context.Catalogos!
                        .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "reclamado")
                        .SingleAsync();

                    foreach (var det in detalles)
                    {
                        var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                        regPalet.Estado = newEstadoPalet.Id.ToString();
                        reg.FechaRecibo = DateTime.UtcNow;
                        regPalet.ApplicationUserId = reg.ApplicationUserIdEnvia;
                    }
                }
                else
                {
                    // Procesamiento normal para transferencias
                    reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
                    reg.FechaRecibo = DateTime.UtcNow;

                    // Obtener todos los detalles de la transferencia
                    var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();
                    var newEstadoPalet = await _context.Catalogos!
                        .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                        .SingleAsync();

                    var estadoReclamado = await _context.Catalogos!
                        .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "reclamado")
                        .SingleOrDefaultAsync();

                    foreach (var det in detalles)
                    {
                        var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();

                        // Validar si el pallet estÃ¡ en estado "Reclamado"
                        if (estadoReclamado != null && regPalet.Estado == estadoReclamado.Id.ToString())
                        {
                            // Registrar que el pallet no puede ser asignado porque ya fue reclamado
                            /*
                            await Utils.SendNotification(
                                viewTrans.UserEnviaFirebaseToken,
                                viewTrans.UserEnviaEmail,
                                viewTrans.UserEnviaFullName,
                                "Pallet reclamado",
                                $"El pallet con ID {regPalet.Id} ya fue reclamado y no puede ser transferido."
                            );
                            */
                            _notification.Success($"El {regPalet.Descripcion} ya fue reclamado, por lo tanto, no se te fue asignado");
                            continue; // Pasar al siguiente pallet
                        }

                        // Si el pallet no estÃ¡ reclamado, asignarlo al usuario receptor
                        regPalet.Estado = newEstadoPalet.Id.ToString();
                        regPalet.ApplicationUserId = reg.ApplicationUserIdRecibe;
                    }

                }
                ret = true;
            }
            else if (operacion == "rechazar")
            {
                // Rechazar tanto transferencia como reclamo
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
                reg.FechaRechazo = DateTime.UtcNow;
                tipo = reg.Estado == listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault()!.Id.ToString()
                    ? "reclamo"
                    : "transferencia";

                var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();
                var newEstadoPalet = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                    .SingleAsync();

                foreach (var det in detalles)
                {
                    var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                    regPalet.Estado = newEstadoPalet.Id.ToString();
                }

                ret = true;
            }
            else if (operacion == "anular")
            {
                // Anular tanto transferencia como reclamo
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
                reg.FechaAnulado = DateTime.UtcNow;
                tipo = reg.Estado == listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault()!.Id.ToString()
                    ? "reclamo"
                    : "transferencia";

                var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();
                var newEstadoPalet = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                    .SingleAsync();

                foreach (var det in detalles)
                {
                    var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                    regPalet.Estado = newEstadoPalet.Id.ToString();
                }

                ret = true;
            }

            if (vm.FotoFile != null)
            {
                reg.Foto = UploadImage(vm.FotoFile);
            }

            await _context.SaveChangesAsync();

            return (ret, tipo);
        }



        // este es de transferencia unicamente, el original
        /*
        private async Task<bool> procesarTransfer(TransferenciaVM vm, string operacion)
        {
            bool ret = false;

            var reg = await _context.Transferencias!.Where(x => x.Id == vm.Id).SingleAsync();
            reg!.Observaciones = vm.Observaciones;

            if (operacion == "aceptar")
            {
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
                reg.FechaRecibo = DateTime.Now;

                var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();

                var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                foreach (var det in detalles)
                {
                    var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                    regPalet.Estado = newEstadoPalet.Id.ToString();
                    regPalet.ApplicationUserId = reg.ApplicationUserIdRecibe;
                   
                }

            }
            else if (operacion == "rechazar")
            {
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
                reg.FechaRechazo = DateTime.Now;

                var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();

                var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                foreach (var det in detalles)
                {
                    var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                    regPalet.Estado = newEstadoPalet.Id.ToString();

                }
            }
            else if (operacion == "anular")
            {
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
                reg.FechaAnulado = DateTime.Now;

                var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == vm.Id).ToListAsync();

                var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                foreach (var det in detalles)
                {
                    var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                    regPalet.Estado = newEstadoPalet.Id.ToString();

                }
            }

            if (vm.FotoFile != null)
            {
                reg.Foto = UploadImage(vm.FotoFile);
            }


            await _context.SaveChangesAsync();


            // despues de actualizar el estado de la transferencia se envian las notificaciones correspondientes

            var viewTrans = await _context.TransferenciasView!.Where(x => x.Id == vm.Id).SingleAsync();

            if (operacion == "aceptar")
            {
                
                await Utils.SendNotification(viewTrans.UserEnviaFirebaseToken,
                                       viewTrans.UserEnviaEmail,
                                       viewTrans.UserEnviaFullName,
                                       "Transferencia aceptada",
                                       "El usuario " + viewTrans.UserRecibeFullName +
                                       " ha aceptado la transferencia con codigo '" +
                                       viewTrans.CodigoInterno + "'");



            }
            else if (operacion == "rechazar")
            {
                
                await Utils.SendNotification(viewTrans.UserEnviaFirebaseToken,
                                       viewTrans.UserEnviaEmail,
                                       viewTrans.UserEnviaFullName,
                                       "Transferencia rechazada",
                                       "El usuario " + viewTrans.UserRecibeFullName +
                                       " ha rechazado la transferencia con codigo '" +
                                       viewTrans.CodigoInterno + "'");

            }



            return true;

        }
        */


        private string UploadImage(IFormFile file)
        {
            string uniqueFileName = "";
            var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "thumbnails");
            uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(folderPath, uniqueFileName);
            using (FileStream fileStream = System.IO.File.Create(filePath))
            {
                file.CopyTo(fileStream);
            }
            return uniqueFileName;
        }

        private async Task<TransferenciaVM> getTransferVM(int id)
        {

            var reg = await _context.Transferencias!.Where(x => x.Id == id).SingleOrDefaultAsync();
            var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == id).ToListAsync();

            if (reg == null)
            {
                throw new InvalidOperationException("Transferencia no encontrada.");
            }

            var userEnvia = _userManager.Users.FirstOrDefault(x => x.Id == reg.ApplicationUserIdEnvia);
            var userRecibe = _userManager.Users.FirstOrDefault(x => x.Id == reg!.ApplicationUserIdRecibe);

            var ll = from dr in detalles
                     from dp in _context.Palets!
                     where dr.IdPalet == dp.Id
                     select new PaletVM
                     {
                         Id = dp.Id,
                         Descripcion = dp.Descripcion ?? string.Empty,
                     };


            var regVM = new TransferenciaVM
            {
                Id = reg.Id,
                CodigoInterno = reg.CodigoInterno,
                DescEstado = listEstadosTrans.Where(x => x.Id.ToString() == reg.Estado).SingleOrDefault()!.Descripcion,
                Estado = reg.Estado,
                FechaEnvio = reg.FechaEnvio,
                FechaRechazo = reg.FechaRechazo,
                FechaRecibo = reg.FechaRecibo,
                FechaAnulado = reg.FechaAnulado,
                IdUserEnvia = reg.ApplicationUserIdEnvia ?? string.Empty,
                IdUserRecibe = reg.ApplicationUserIdRecibe ?? string.Empty,
                NombreUserEnvia = userEnvia != null ? userEnvia.Nombres + " " + userEnvia.Apellidos : "Sin asignar",
                //NombreUserRecibe = userRecibe!.Nombres + " " + userRecibe!.Apellidos,

                // Esta linea se modifico ya que me muestre los reclamos que solo acceden los administradores
                NombreUserRecibe = reg.ApplicationUserIdRecibe == "Administradores"
                        ? "Administradores"
                        : (userRecibe != null ? userRecibe.Nombres + " " + userRecibe.Apellidos : "Sin asignar"),

                Observaciones = reg.Observaciones,
                Foto = reg.Foto,
                Palets = ll.ToList()
            };

            return regVM;

        }



        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var loggedInUser = _userManager.Users.FirstOrDefault(x => x.UserName == User.Identity!.Name);

            var regVM = await getTransferVM(id);

            ViewBag.estadoRecibido = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoRechazado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoPorRecibir = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por recibir").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoPorReclamar = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault()!.Id.ToString();

            ViewBag.estadoAnulado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
            ViewBag.loggedInUserId = loggedInUser!.Id;

            return View(regVM);

        }

        [HttpPost]
        public async Task<IActionResult> Detalle(TransferenciaVM vm, string submit)
        {
            switch (submit)
            {
                case "aceptar":
                    var (successAceptar, tipoAceptar) = await procesarTransfer(vm, submit);
                    if (successAceptar)
                    {
                        _notification.Success($"El {tipoAceptar} fue aceptado exitosamente");
                        return RedirectToAction("Index");
                    }
                    break;
                case "rechazar":
                    var (successRechazar, tipoRechazar) = await procesarTransfer(vm, submit);
                    if (successRechazar)
                    {
                        _notification.Success($"El {tipoRechazar} fue rechazado exitosamente");
                        return RedirectToAction("Index");
                    }
                    break;
                case "anular":
                    var (successAnular, tipoAnular) = await procesarTransfer(vm, submit);
                    if (successAnular)
                    {
                        _notification.Success($"El {tipoAnular} fue anulado exitosamente");
                        return RedirectToAction("Index");
                    }
                    break;
            }

            var loggedInUser = _userManager.Users.FirstOrDefault(x => x.UserName == User.Identity!.Name);

            var regVM = await getTransferVM(vm.Id);
            regVM.Observaciones = vm.Observaciones;

            ViewBag.estadoRecibido = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoReclamado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "reclamado").SingleOrDefault()?.Id.ToString() ?? string.Empty;
            ViewBag.estadoRechazado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoPorRecibir = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por recibir").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoPorReclamar = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoAnulado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
            ViewBag.loggedInUserId = loggedInUser!.Id;

            return View(regVM);
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




