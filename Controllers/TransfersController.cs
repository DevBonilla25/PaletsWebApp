using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Services;
using PaletsWebApp.Utilites;
using PaletsWebApp.ViewModels;
using System.Diagnostics;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace PaletsWebApp.Controllers
{
    [Authorize]
    public class TransfersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public INotyfService _notification { get; }
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TransferenciaQueryService _transferenciaQueryService;
        private readonly DetalleTransferenciaService _detalleTransferenciaService;
        private readonly ReclamoService _reclamoService;

        List<Catalogo> listEstadosTrans;
        private const string ReceptorAdministradores = "Administradores";

        public TransfersController(ApplicationDbContext context,
                                INotyfService notyfService,
                                IWebHostEnvironment webHostEnvironment,
                                UserManager<ApplicationUser> userManager,
                                TransferenciaQueryService transferenciaQueryService,
                                DetalleTransferenciaService detalleTransferenciaService,
                                ReclamoService reclamoService)
        {
            _context = context;
            _notification = notyfService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _transferenciaQueryService = transferenciaQueryService;
            _detalleTransferenciaService = detalleTransferenciaService;
            _reclamoService = reclamoService;

            listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();

        }

        // Lista transferencias para Razor usando los filtros y paginación compartidos.
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
            string filterPalet,
            string sPalet,
            int? palletId,
            int? pageNumber,
            int pageSize = 15)
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

            if (sPalet != null) pageNumber = 1;
            else sPalet = filterPalet;
            
            ViewData["filterUserEnvia"] = sUserEnvia;
            ViewData["filterUserRecibe"] = sUserRecibe;
            ViewData["filterDate1"] = sDate1;
            ViewData["filterDate2"] = sDate2;
            ViewData["filterEstado"] = sEstado;
            ViewData["filterPalet"] = sPalet;
            ViewData["PalletId"] = palletId;



            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            if (loggedInUser == null)
            {
                return Unauthorized();
            }
            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);

            // Determinar si el usuario puede reclamar palets
            ViewBag.CanReclaimPalets = !loggedInUserRole.Contains(WebsiteRoles.Cliente);

            if (palletId.HasValue)
            {
                var palletQuery = _context.PaletsView!.AsNoTracking().Where(x => x.Id == palletId.Value);
                if (!loggedInUserRole.Contains(WebsiteRoles.Admin) &&
                    !loggedInUserRole.Contains(WebsiteRoles.Supervisor))
                {
                    palletQuery = palletQuery.Where(x => x.ApplicationUserId == loggedInUser.Id);
                }
                ViewBag.HistoryPallet = await palletQuery.SingleOrDefaultAsync();
                if (ViewBag.HistoryPallet == null) return NotFound();
            }

            DateTime? fechaDesde = null;
            DateTime? fechaHasta = null;
            if (!String.IsNullOrEmpty(sDate1))
            {
                var cultureInfo = new CultureInfo("es-ES");
                fechaDesde = DateTime.Parse(sDate1, cultureInfo);
            }

            if (!String.IsNullOrEmpty(sDate2))
            {
                var cultureInfo = new CultureInfo("es-ES");
                fechaHasta = DateTime.Parse(sDate2, cultureInfo).AddDays(1).AddSeconds(-1);
            }

            ViewBag.estadoRecibido = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoReclamado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "reclamado").SingleOrDefault()?.Id.ToString() ?? string.Empty;
            ViewBag.estadoRechazado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoAnulado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
            ViewBag.EstadosTransferencia = listEstadosTrans.OrderBy(x => x.Descripcion).ToList();


       

            pageSize = new[] { 15, 25, 50 }.Contains(pageSize) ? pageSize : 15;
            ViewData["PageSize"] = pageSize;
            // El servicio ejecuta los mismos filtros de visibilidad y orden usados por la API.
            var page = await _transferenciaQueryService.GetPageAsync(new TransferenciaQuery
            {
                UserId = loggedInUser.Id,
                IsAdmin = loggedInUserRole.Contains(WebsiteRoles.Admin) ||
                          loggedInUserRole.Contains(WebsiteRoles.Supervisor),
                UserEnvia = sUserEnvia,
                UserRecibe = sUserRecibe,
                Estado = sEstado,
                Palet = sPalet,
                PaletId = palletId,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                SortOrder = sortOrder,
                Page = pageNumber ?? 1,
                PageSize = pageSize
            }, includeTotalCount: true);
            var paginatedTransfers = new PaginatedList<View_Transferencia>(
                page.Items, page.TotalCount, page.Page, page.PageSize);

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
                    transfer.CodigoInterno = transfer.CodigoInterno;
                }
            }

            return View(paginatedTransfers);


        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Obtén los IDs de los estados "disponible" y "reclamados"
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

            var regUserDestino = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == vm.IdUserRecibe);
            if (loggedInUser == null || regUserDestino == null)
            {
                return NotFound();
            }
            var defaultEstado = listEstadosTrans.SingleOrDefault(x =>
                x.Descripcion!.ToLower() == "por recibir");
            if (defaultEstado == null)
            {
                ModelState.AddModelError("", "No está configurado el estado 'por recibir'.");
                return View(vm);
            }

            var reg = new Transferencia();

            //Estas fechas obtienes la hora del sistema operativo, por lo que en produccion toma la hora del servidor externo y no es el mismo del Ecuador
            //reg.CodigoInterno = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            //reg.FechaEnvio = DateTime.Now;

            // Cambiar DateTime.Now por DateTime.UtcNow para Ecuador
            reg.FechaEnvio = DateTime.UtcNow; // Guardar en UTC
            reg.FechaLimiteAceptacion = DateTime.UtcNow.AddHours(48);
            reg.ApplicationUserIdEnvia = loggedInUser!.Id;
            reg.ApplicationUserIdRecibe = vm.IdUserRecibe;
            reg.Estado = defaultEstado!.Id.ToString();


            await _context.Transferencias!.AddAsync(reg);

            await _context.SaveChangesAsync();
            reg.CodigoInterno = $"TRF-{reg.Id}";

            var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();
            var estadoDetallePendiente = await _detalleTransferenciaService
                .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);

            var idsPalets = vm.UqChecked!
                .Select(int.Parse)
                .Distinct()
                .ToList();
            var palets = await _context.Palets!
                .Where(x => idsPalets.Contains(x.Id))
                .ToListAsync();
            var detalles = idsPalets.Select(idPalet => new Detalle
                {
                    IdPalet = idPalet,
                    IdTransferencia = reg.Id,
                    Estado = estadoDetallePendiente,
                    FechaEstado = DateTime.UtcNow
                })
                .ToList();

            await _context.Detalles!.AddRangeAsync(detalles);
            foreach (var palet in palets)
            {
                palet.Estado = newEstadoPalet.Id.ToString();
            }

            await _context.SaveChangesAsync();

            await Utils.SendNotification(regUserDestino.FirebaseToken,
                                             regUserDestino.Email ?? string.Empty,
                                             regUserDestino.Nombres + " " + regUserDestino.Apellidos,
                                             "Has recibido una transferencia ",
                                             "El usuario " + loggedInUser.Nombres + " " + loggedInUser.Apellidos + " te ha realizado la transferencia con codigo '" + reg.CodigoInterno + "'");

            _notification.Success("Transferencia creada exitosamente");
            return RedirectToAction("Index");
        }



        // Busca pallets y reconstruye la selección acumulada usando solo sus IDs.
        [HttpGet]
        [Authorize(Roles = "Admin,Supervisor,Bodeguero,Chofer")]
        public async Task<IActionResult> Reclamar(
            string sDescripcion, 
            string selectedPaletIds)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            if (loggedInUser == null)
            {
                return Unauthorized();
            }
            var regVM = new TransferenciaVM();

            var idsSeleccionados = (selectedPaletIds ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .Take(100)
                .ToList();

            // Solo viajan IDs en la URL; los datos visibles se reconstruyen desde la base.
            var selectedPalets = await _context.PaletsView!.AsNoTracking()
                .Where(x => idsSeleccionados.Contains(x.Id))
                .OrderBy(x => x.Descripcion)
                .Select(x => new PaletVM
                {
                    Id = x.Id,
                    Descripcion = x.Descripcion ?? string.Empty,
                    Estado = x.Estado,
                    DescEstado = x.DescEstado,
                    ApplicationUserId = x.ApplicationUserId,
                    ApplicationUserName = x.UserFullName,
                    IsSelected = true
                })
                .ToListAsync();
            List<PaletVM> resultadosBusqueda = new();

            // Si no hay filtro, no cargues pallets adicionales
            if (!string.IsNullOrWhiteSpace(sDescripcion))
            {
                var termino = sDescripcion.Trim();
                var digitos = new string(termino.Where(char.IsDigit).ToArray());
                var textoRestante = termino.ToLowerInvariant()
                    .Replace("pallet", string.Empty)
                    .Replace("palet", string.Empty)
                    .Replace("#", string.Empty)
                    .Replace(" ", string.Empty);
                int? numeroPalet = textoRestante.Length == 0 && int.TryParse(digitos, out var numero)
                    ? numero
                    : null;
                var numeroSinCeros = numeroPalet?.ToString();
                var numeroTresDigitos = numeroPalet?.ToString("D3");

                var idsEstadosVisibles = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" &&
                                x.Descripcion!.ToLower() != "dado de baja")
                    .Select(x => x.Id.ToString())
                    .ToListAsync();

                resultadosBusqueda = await _context.PaletsView!.AsNoTracking()
                    .Where(x => x.ApplicationUserId != loggedInUser.Id &&
                                x.Descripcion != null &&
                                idsEstadosVisibles.Contains(x.Estado!) &&
                                (EF.Functions.Like(x.Descripcion, $"%{termino}%") ||
                                 (numeroPalet.HasValue &&
                                  (EF.Functions.Like(x.Descripcion, $"%#{numeroSinCeros}") ||
                                   EF.Functions.Like(x.Descripcion, $"%#{numeroTresDigitos}")))))
                    .OrderBy(x => x.Descripcion)
                    .Take(20)
                    .Select(x => new PaletVM
                    {
                        Id = x.Id,
                        Descripcion = x.Descripcion ?? string.Empty,
                        Estado = x.Estado,
                        DescEstado = x.DescEstado,
                        ApplicationUserId = x.ApplicationUserId,
                        ApplicationUserName = x.UserFullName
                    })
                    .ToListAsync();

                var idsEnTransferencia = resultadosBusqueda
                    .Where(x => string.Equals(x.DescEstado, "En transferencia", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Id)
                    .ToList();
                if (idsEnTransferencia.Count > 0)
                {
                    var pendienteId = await _detalleTransferenciaService
                        .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
                    var estadoPorRecibir = listEstadosTrans
                        .Single(x => x.Descripcion!.ToLower() == "por recibir").Id.ToString();
                    var origenes = await (from detalle in _context.Detalles!.AsNoTracking()
                                          join transferencia in _context.TransferenciasView!.AsNoTracking()
                                              on detalle.IdTransferencia equals transferencia.Id
                                          where idsEnTransferencia.Contains(detalle.IdPalet) &&
                                                (detalle.Estado == pendienteId || detalle.Estado == string.Empty) &&
                                                transferencia.Estado == estadoPorRecibir
                                          select new
                                          {
                                              detalle.IdPalet,
                                              detalle.IdTransferencia,
                                              transferencia.ApplicationUserIdRecibe,
                                              transferencia.UserRecibeFullName
                                          }).ToListAsync();

                    resultadosBusqueda = resultadosBusqueda
                        .Where(palet => !idsEnTransferencia.Contains(palet.Id) ||
                                        origenes.Any(origen => origen.IdPalet == palet.Id &&
                                            origen.ApplicationUserIdRecibe != loggedInUser.Id))
                        .ToList();
                    foreach (var palet in resultadosBusqueda.Where(x => idsEnTransferencia.Contains(x.Id)))
                    {
                        var origen = origenes.First(x => x.IdPalet == palet.Id);
                        palet.EsCorreccionCustodia = true;
                        palet.TransferenciaPendienteId = origen.IdTransferencia;
                        palet.ReceptorTransferenciaPendiente = origen.UserRecibeFullName;
                    }
                }

                foreach (var palet in resultadosBusqueda)
                {
                    palet.IsSelected = selectedPalets.Any(x => x.Id == palet.Id);
                }
            }

            regVM.Palets = selectedPalets;
            regVM.JsonPalets = JsonSerializer.Serialize(selectedPalets);
            ViewBag.ResultadosBusqueda = resultadosBusqueda;
            ViewBag.EstadoReclamado = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "reclamado")
                .Select(x => x.Id.ToString())
                .SingleOrDefaultAsync() ?? string.Empty;
            ViewData["filterDescripcion"] = sDescripcion;

            ViewBag.PalletRequired = false;

            return View(regVM);
        }


        // Valida y crea un reclamo atómico con todos los pallets seleccionados.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Supervisor,Bodeguero,Chofer")]
        public async Task<IActionResult> Reclamar(TransferenciaVM vm)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            if (loggedInUser == null)
            {
                return Unauthorized();
            }

            var idsPalets = vm.UqChecked
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            ViewBag.PalletRequired = false;

            // Validar que al menos un pallet está seleccionado
            if (idsPalets.Count == 0)
            {
                var list_paletsVM = JsonSerializer.Deserialize<List<PaletVM>>(vm.JsonPalets ?? "[]") ?? new List<PaletVM>();
                vm.Palets = list_paletsVM;
                ViewBag.PalletRequired = true;

                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(vm.Observaciones))
            {
                ModelState.AddModelError(nameof(vm.Observaciones), "El motivo del reclamo es requerido.");
                vm.Palets = JsonSerializer.Deserialize<List<PaletVM>>(vm.JsonPalets ?? "[]") ?? new List<PaletVM>();
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

            // La transacción permite varios reclamantes, pero evita duplicados del mismo usuario.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
            if (palets.Count != idsPalets.Count)
            {
                return BadRequest("Uno o más pallets no existen.");
            }

            var estadoDadoBaja = await _context.Catalogos!
                .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "dado de baja")
                .Select(x => x.Id.ToString())
                .SingleOrDefaultAsync();
            var paletsInvalidos = palets
                .Where(x => x.ApplicationUserId == loggedInUser.Id || x.Estado == estadoDadoBaja)
                .ToList();
            if (paletsInvalidos.Count > 0)
            {
                var detalle = string.Join(", ", paletsInvalidos.Select(x => x.Descripcion));
                ModelState.AddModelError("", $"No puedes reclamar pallets propios o dados de baja: {detalle}.");
                vm.Palets = JsonSerializer.Deserialize<List<PaletVM>>(vm.JsonPalets ?? "[]") ?? new List<PaletVM>();
                return View(vm);
            }

            var estadoDetallePendiente = await _detalleTransferenciaService
                .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
            var reclamosDuplicados = await (from detalle in _context.Detalles!
                                        join transferencia in _context.Transferencias!
                                            on detalle.IdTransferencia equals transferencia.Id
                                        where idsPalets.Contains(detalle.IdPalet) &&
                                              (detalle.Estado == estadoDetallePendiente || detalle.Estado == string.Empty) &&
                                              transferencia.ApplicationUserIdEnvia == loggedInUser.Id &&
                                              transferencia.ApplicationUserIdRecibe == ReceptorAdministradores
                                        select detalle.IdPalet).Distinct().ToListAsync();
            if (reclamosDuplicados.Count > 0)
            {
                var nombres = palets.Where(x => reclamosDuplicados.Contains(x.Id)).Select(x => x.Descripcion);
                ModelState.AddModelError("", $"Ya tienes un reclamo pendiente para: {string.Join(", ", nombres)}.");
                vm.Palets = JsonSerializer.Deserialize<List<PaletVM>>(vm.JsonPalets ?? "[]") ?? new List<PaletVM>();
                return View(vm);
            }

            // Crear el registro de transferencia para el reclamo
            var reg = new Transferencia
            {
                // Cambiar DateTime.Now por DateTime.UtcNow para Ecuador
                FechaEnvio = DateTime.UtcNow, // Guardar en UTC
                ApplicationUserIdEnvia = loggedInUser!.Id,
                ApplicationUserIdRecibe = ReceptorAdministradores,
                Estado = estadoPorReclamar.Id.ToString(),
                Observaciones = vm.Observaciones.Trim()
            };

            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();
            reg.CodigoInterno = $"TRF-{reg.Id}";

            await _context.Detalles!.AddRangeAsync(palets.Select(palet => new Detalle
            {
                IdPalet = palet.Id,
                IdTransferencia = reg.Id,
                Estado = estadoDetallePendiente,
                FechaEstado = DateTime.UtcNow,
                EstadoPaletAnterior = palet.Estado,
                ApplicationUserIdCustodioAnterior = palet.ApplicationUserId
            }));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            reg.CodigoInterno = $"TRF-{reg.Id}";

            await _reclamoService.NotificarSupervisoresAsync(loggedInUser, reg.Id);

            _notification.Success("Reclamo creado exitosamente");
            return RedirectToAction("Index");
        }



        // Aqui procesamos si es Transferwencia o Reclamo
        private async Task<(bool Success, string Tipo)> procesarTransfer(
            TransferenciaVM vm,
            string operacion,
            string usuarioId)
        {
            bool ret = false;
            string tipo = "transferencia";

            var reg = await _context.Transferencias!.Where(x => x.Id == vm.Id).SingleAsync();
            reg!.Observaciones = vm.Observaciones;
            var estadoPorReclamar = listEstadosTrans.Single(x => x.Descripcion!.ToLower() == "por reclamar").Id.ToString();
            var esReclamo = reg.Estado == estadoPorReclamar;

            if (operacion == "aceptar")
            {
                if (esReclamo)
                {
                    tipo = "reclamo";
                    await _reclamoService.AdjudicarAsync(vm.Id, usuarioId, vm.Observaciones);
                }
                else
                {
                    // Procesamiento normal para transferencias

                    // Obtener todos los detalles de la transferencia
                    var detalles = await _detalleTransferenciaService.GetPendientesAsync(vm.Id);
                    var idsPalets = detalles.Select(x => x.IdPalet).ToList();
                    var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
                    var newEstadoPalet = await _context.Catalogos!
                        .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                        .SingleAsync();
                    var estadoDetalle = await _detalleTransferenciaService
                        .GetEstadoIdAsync(DetalleTransferenciaEstados.Recibido);

                    foreach (var regPalet in palets)
                    {
                        // Incluso si antes estaba reclamado, una transferencia aceptada inicia una custodia nueva.
                        regPalet.Estado = newEstadoPalet.Id.ToString();
                        regPalet.ApplicationUserId = reg.ApplicationUserIdRecibe;
                    }
                    DetalleTransferenciaService.CambiarEstado(
                        detalles, estadoDetalle, usuarioId, vm.Observaciones);
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(new[] { reg.Id });

                }
                ret = true;
            }
            else if (operacion == "rechazar")
            {
                // Rechazar tanto transferencia como reclamo
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
                reg.FechaRechazo = DateTime.UtcNow;
                tipo = esReclamo ? "reclamo" : "transferencia";

                var detalles = await _detalleTransferenciaService.GetPendientesAsync(vm.Id);
                var newEstadoPalet = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                    .SingleAsync();
                var estadoPaletEnTransferencia = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia")
                    .SingleAsync();
                var estadoDetalle = await _detalleTransferenciaService
                    .GetEstadoIdAsync(DetalleTransferenciaEstados.Rechazado);
                var idsPalets = detalles.Select(x => x.IdPalet).ToList();
                var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
                var idsOrigen = detalles.Where(x => x.IdDetalleOrigen.HasValue)
                    .Select(x => x.IdDetalleOrigen!.Value).ToList();
                var detallesOrigen = await _context.Detalles!
                    .Where(x => idsOrigen.Contains(x.Id)).ToListAsync();
                var palletsConOrigen = detalles
                    .Where(x => x.IdDetalleOrigen.HasValue).Select(x => x.IdPalet).ToHashSet();

                if (!esReclamo)
                foreach (var regPalet in palets)
                {
                    var detalle = detalles.First(x => x.IdPalet == regPalet.Id);
                    regPalet.Estado = esReclamo && !string.IsNullOrWhiteSpace(detalle.EstadoPaletAnterior)
                        ? detalle.EstadoPaletAnterior
                        : palletsConOrigen.Contains(regPalet.Id)
                            ? estadoPaletEnTransferencia.Id.ToString()
                            : newEstadoPalet.Id.ToString();
                    if (esReclamo)
                        regPalet.ApplicationUserId = detalle.ApplicationUserIdCustodioAnterior;
                }
                DetalleTransferenciaService.CambiarEstado(
                    detalles, estadoDetalle, usuarioId, vm.Observaciones);
                if (!esReclamo && detallesOrigen.Count > 0)
                {
                    var estadoPendiente = await _detalleTransferenciaService
                        .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
                    DetalleTransferenciaService.CambiarEstado(
                        detallesOrigen, estadoPendiente, usuarioId, vm.Observaciones);
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(
                        detallesOrigen.Select(x => x.IdTransferencia));
                }
                if (!esReclamo)
                {
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(new[] { reg.Id });
                }

                ret = true;
            }
            else if (operacion == "anular")
            {
                // Anular tanto transferencia como reclamo
                reg.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
                reg.FechaAnulado = DateTime.UtcNow;
                tipo = esReclamo ? "reclamo" : "transferencia";

                var detalles = await _detalleTransferenciaService.GetPendientesAsync(vm.Id);
                var newEstadoPalet = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                    .SingleAsync();
                var estadoPaletEnTransferencia = await _context.Catalogos!
                    .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia")
                    .SingleAsync();
                var estadoDetalle = await _detalleTransferenciaService
                    .GetEstadoIdAsync(DetalleTransferenciaEstados.Anulado);
                var idsPalets = detalles.Select(x => x.IdPalet).ToList();
                var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
                var idsOrigen = detalles.Where(x => x.IdDetalleOrigen.HasValue)
                    .Select(x => x.IdDetalleOrigen!.Value).ToList();
                var detallesOrigen = await _context.Detalles!
                    .Where(x => idsOrigen.Contains(x.Id)).ToListAsync();
                var palletsConOrigen = detalles
                    .Where(x => x.IdDetalleOrigen.HasValue).Select(x => x.IdPalet).ToHashSet();

                if (!esReclamo)
                foreach (var regPalet in palets)
                {
                    var detalle = detalles.First(x => x.IdPalet == regPalet.Id);
                    regPalet.Estado = esReclamo && !string.IsNullOrWhiteSpace(detalle.EstadoPaletAnterior)
                        ? detalle.EstadoPaletAnterior
                        : palletsConOrigen.Contains(regPalet.Id)
                            ? estadoPaletEnTransferencia.Id.ToString()
                            : newEstadoPalet.Id.ToString();
                    if (esReclamo)
                        regPalet.ApplicationUserId = detalle.ApplicationUserIdCustodioAnterior;
                }
                DetalleTransferenciaService.CambiarEstado(
                    detalles, estadoDetalle, usuarioId, vm.Observaciones);
                if (!esReclamo && detallesOrigen.Count > 0)
                {
                    var estadoPendiente = await _detalleTransferenciaService
                        .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
                    DetalleTransferenciaService.CambiarEstado(
                        detallesOrigen, estadoPendiente, usuarioId, vm.Observaciones);
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(
                        detallesOrigen.Select(x => x.IdTransferencia));
                }
                if (!esReclamo)
                {
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(new[] { reg.Id });
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
            if (reg == null)
            {
                throw new InvalidOperationException("Transferencia no encontrada.");
            }

            var userEnvia = _userManager.Users.FirstOrDefault(x => x.Id == reg.ApplicationUserIdEnvia);
            var userRecibe = _userManager.Users.FirstOrDefault(x => x.Id == reg!.ApplicationUserIdRecibe);

            var palets = await (from detalle in _context.Detalles!.AsNoTracking()
                                join palet in _context.Palets!.AsNoTracking()
                                    on detalle.IdPalet equals palet.Id
                                join estado in _context.Catalogos!.AsNoTracking()
                                    on detalle.Estado equals estado.Id.ToString() into estados
                                from estado in estados.DefaultIfEmpty()
                                where detalle.IdTransferencia == id
                                select new PaletVM
                                {
                                    Id = palet.Id,
                                    Descripcion = palet.Descripcion ?? string.Empty,
                                    EstadoDetalle = detalle.Estado,
                                    DescEstadoDetalle = estado != null ? estado.Descripcion : null,
                                    FechaEstadoDetalle = detalle.FechaEstado,
                                    ObservacionesDetalle = detalle.Observaciones,
                                    IdDetalleOrigen = detalle.IdDetalleOrigen,
                                    ApplicationUserIdResuelveDetalle = detalle.ApplicationUserIdResuelve
                                }).ToListAsync();
            var idsUsuariosResuelven = palets
                .Select(x => x.ApplicationUserIdResuelveDetalle)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            var nombresUsuariosResuelven = await _userManager.Users.AsNoTracking()
                .Where(x => idsUsuariosResuelven.Contains(x.Id))
                .Select(x => (x.Nombres + " " + x.Apellidos).Trim())
                .ToListAsync();


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
                NombreUserRecibe = reg.ApplicationUserIdRecibe == ReceptorAdministradores
                        ? "Supervisores"
                        : (userRecibe != null ? userRecibe.Nombres + " " + userRecibe.Apellidos : "Sin asignar"),
                NombreUsuariosResuelven = string.Join(", ", nombresUsuariosResuelven),

                Observaciones = reg.Observaciones,
                Foto = reg.Foto,
                Palets = palets
            };

            return regVM;

        }



        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var loggedInUser = _userManager.Users.FirstOrDefault(x => x.UserName == User.Identity!.Name);

            var regVM = await getTransferVM(id);

            ViewBag.estadoRecibido = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
            ViewBag.estadoReclamado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "reclamado").SingleOrDefault()?.Id.ToString() ?? string.Empty;
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
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var transferencia = await _context.Transferencias!.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vm.Id);
            if (loggedInUser == null || transferencia == null)
            {
                return NotFound();
            }

            var esAdmin = await _userManager.IsInRoleAsync(loggedInUser, WebsiteRoles.Admin!) ||
                          await _userManager.IsInRoleAsync(loggedInUser, WebsiteRoles.Supervisor!);
            var estadoPorRecibir = listEstadosTrans.Single(x => x.Descripcion!.ToLower() == "por recibir").Id.ToString();
            var estadoPorReclamar = listEstadosTrans.Single(x => x.Descripcion!.ToLower() == "por reclamar").Id.ToString();
            var esReclamo = transferencia.Estado == estadoPorReclamar;
            var esTransferenciaPendiente = transferencia.Estado == estadoPorRecibir;

            var puedeResolver = esReclamo
                ? esAdmin && (submit == "aceptar" || submit == "rechazar")
                : esTransferenciaPendiente && transferencia.ApplicationUserIdRecibe == loggedInUser.Id &&
                  (submit == "aceptar" || submit == "rechazar");
            var puedeAnular = submit == "anular" &&
                ((esReclamo && transferencia.ApplicationUserIdEnvia == loggedInUser.Id) ||
                 (esTransferenciaPendiente &&
                  (transferencia.ApplicationUserIdEnvia == loggedInUser.Id || esAdmin)));

            if (!puedeResolver && !puedeAnular)
            {
                return Forbid();
            }

            switch (submit)
            {
                case "aceptar":
                    var (successAceptar, tipoAceptar) = await procesarTransfer(vm, submit, loggedInUser.Id);
                    if (successAceptar)
                    {
                        if (tipoAceptar == "reclamo")
                        {
                            await _reclamoService.NotificarResultadoAsync(vm.Id, loggedInUser.Id, aceptado: true);
                        }
                        _notification.Success($"El {tipoAceptar} fue aceptado exitosamente");
                        return RedirectToAction("Index");
                    }
                    break;
                case "rechazar":
                    var (successRechazar, tipoRechazar) = await procesarTransfer(vm, submit, loggedInUser.Id);
                    if (successRechazar)
                    {
                        if (tipoRechazar == "reclamo")
                        {
                            await _reclamoService.NotificarResultadoAsync(vm.Id, loggedInUser.Id, aceptado: false);
                        }
                        _notification.Success($"El {tipoRechazar} fue rechazado exitosamente");
                        return RedirectToAction("Index");
                    }
                    break;
                case "anular":
                    var (successAnular, tipoAnular) = await procesarTransfer(vm, submit, loggedInUser.Id);
                    if (successAnular)
                    {
                        _notification.Success($"El {tipoAnular} fue anulado exitosamente");
                        return RedirectToAction("Index");
                    }
                    break;
            }

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






