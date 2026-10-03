using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Services;
using PaletsWebApp.Utilites;
using PaletsWebApp.ViewModels;
using System.Diagnostics;
using System.Data;
using System.Text.Json;

namespace PaletsWebApp.Controllers
{
    public class ApiController : Controller
    {
        private readonly ApplicationDbContext _context;
        public INotyfService _notification { get; }
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TransferenciaQueryService _transferenciaQueryService;
        private readonly DetalleTransferenciaService _detalleTransferenciaService;
        private readonly ReclamoService _reclamoService;

        public ApiController(ApplicationDbContext context,
                             INotyfService notyfService,
                             UserManager<ApplicationUser> userManager,
                             RoleManager<IdentityRole> roleManager,
                             TransferenciaQueryService transferenciaQueryService,
                             DetalleTransferenciaService detalleTransferenciaService,
                             ReclamoService reclamoService)
        {
            _context = context;
            _notification = notyfService;
            _userManager = userManager;
            _roleManager = roleManager;
            _transferenciaQueryService = transferenciaQueryService;
            _detalleTransferenciaService = detalleTransferenciaService;
            _reclamoService = reclamoService;
        }


        [HttpGet]
        [Route("api/ApiAccess/Login")]
        public async Task<IActionResult> Login(string user,
                                                              string password, 
                                                              string firebaseToken)
        {

            RegisterUserVM vm = new RegisterUserVM();

            try
            {

                var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == user);

                if (regUser == null)
                {
                    return Unauthorized(ApiResponse.Failed("Usuario o contraseña incorrectos.", "INVALID_CREDENTIALS"));
                }

                if (regUser != null)
                {

                    if (regUser.Activo == false)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            ApiResponse.Failed("La cuenta todavía no ha sido activada.", "ACCOUNT_INACTIVE"));
                    }

                    var verifyPassword = await _userManager.CheckPasswordAsync(regUser, password);
                    if (!verifyPassword)
                    {
                        return Unauthorized(ApiResponse.Failed("Usuario o contraseña incorrectos.", "INVALID_CREDENTIALS"));
                    }

                    var roles = await _userManager.GetRolesAsync(regUser!);
                    string rolUser = roles.FirstOrDefault()!;

                    vm.Id = regUser.Id;
                    vm.Nombres = regUser.Nombres;
                    vm.Apellidos = regUser.Apellidos;
                    vm.TipoDocumento = regUser.TipoDocumento;
                    vm.Email = regUser.Email;
                    vm.UserName = regUser.UserName;
                    vm.Documento = regUser.Documento;
                    vm.Telefono = regUser.Telefono;
                    vm.Direccion = regUser.Direccion;
                    vm.FirebaseToken = firebaseToken;
                    vm.Rol = rolUser;

                    if (!string.IsNullOrEmpty(firebaseToken))
                    {
                        if (regUser.FirebaseToken != firebaseToken)
                        {
                            regUser.FirebaseToken = firebaseToken;
                            await _userManager.UpdateAsync(regUser);
                        }
                    }
                    
                }
                else
                    return Unauthorized(ApiResponse.Failed("Usuario o contraseña incorrectos.", "INVALID_CREDENTIALS"));


            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Failed("Ocurrió un error inesperado al iniciar sesión.", "LOGIN_ERROR"));
            }

            return Ok(ApiResponse.Succeeded(vm, "Inicio de sesión exitoso."));

        }

        // Devuelve la lusta de todos los usuarios
        [HttpGet]
        [Route("api/ApiAccess/GetUsers")]
        public async Task<IActionResult> GetUsers(string userId)
        {

            List<RegisterUserVM> lista = new List<RegisterUserVM>();

            try
            {

                var Usuarios = from regUser in _context.UsersView 
                               where regUser.Id != userId
                               select new RegisterUserVM {

                                   Id = regUser.Id,
                                   Nombres = regUser.Nombres,
                                   Apellidos = regUser.Apellidos,
                                   TipoDocumento = regUser.TipoDocumento,
                                   Email = regUser.Email,
                                   UserName = regUser.UserName,
                                   Documento = regUser.Documento,
                                   Telefono = regUser.Telefono,
                                   Direccion = regUser.Direccion,
                                   Rol = regUser.RolName,
                                   RazonSocial = regUser.RazonSocial,
                                   FirebaseToken = regUser.FirebaseToken,

                               };

                lista = await Usuarios.ToListAsync();

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Failed("Ocurrió un error al consultar los usuarios.", "USERS_QUERY_ERROR"));
            }

            return Ok(ApiResponse.Succeeded(lista, "Usuarios obtenidos con éxito."));

        }



        // Esta consulta devuelve a un usuario enviado por parametro Id
        [HttpGet]
        [Route("api/ApiAccess/GetUserById")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            RegisterUserVM? usuario = null;

            try
            {
                // Buscar el usuario por su Id
                usuario = await (from regUser in _context.UsersView
                                 where regUser.Id == userId
                                 select new RegisterUserVM
                                 {
                                     Id = regUser.Id,
                                     Nombres = regUser.Nombres,
                                     Apellidos = regUser.Apellidos,
                                     TipoDocumento = regUser.TipoDocumento,
                                     Email = regUser.Email,
                                     UserName = regUser.UserName,
                                     Documento = regUser.Documento,
                                     Telefono = regUser.Telefono,
                                     Direccion = regUser.Direccion,
                                     Rol = regUser.RolName,
                                     RazonSocial = regUser.RazonSocial,
                                     FirebaseToken = regUser.FirebaseToken
                                 }).FirstOrDefaultAsync();

                // Si no se encuentra el usuario, retornar un mensaje 404
                if (usuario == null)
                {
                    return NotFound(ApiResponse.Failed("El usuario no existe.", "USER_NOT_FOUND"));
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Failed("Ocurrió un error al consultar el usuario.", "USER_QUERY_ERROR"));
            }

            // Retornar el usuario encontrado
            return Ok(ApiResponse.Succeeded(usuario, "Usuario obtenido con éxito."));
        }




        // Este codigo llama las primeras 10 datos de palets por Usario, haciendo paginado
        [HttpGet]
        [Route("api/ApiAccess/GetPaletsByUser")]
        public async Task<IActionResult> getPaletsByUserId(string userId, string searchTerm, int page = 1, int pageSize = 10)
        {

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (loggedInUser == null)
                return NotFound(ApiResponse.Failed("El usuario no existe.", "USER_NOT_FOUND"));

            var Palets = from s in _context.PaletsView select s;

            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if (!loggedInUserRole.Contains(WebsiteRoles.Admin) &&
                !loggedInUserRole.Contains(WebsiteRoles.Supervisor))
            {
                Palets = from s in Palets
                         where s.ApplicationUserId == loggedInUser!.Id
                         select s;
            }


            if (!string.IsNullOrEmpty(searchTerm))
            {
                Palets = from s in Palets
                         where s.UserFullName!.Contains(searchTerm) ||
                                  s.Descripcion!.Contains(searchTerm) ||
                                  s.DescEstado!.Contains(searchTerm)
                         select s;
            }


            var listOfPaletsVM = Palets
                .Skip((page - 1) * pageSize)  // Saltar los registros anteriores
                .Take(pageSize)  // Tomar solo el número de registros según pageSize
                .Select(x => new PaletVM()
            {
                Id = x.Id,
                Descripcion = x.Descripcion!,
                FechaCreacion = x.FechaCreacion,
                Observaciones = x.Observaciones,
                Estado = x.Estado,
                DescEstado = x.DescEstado,
                ApplicationUserId = x.ApplicationUserId,
                ApplicationUserName = x.UserFullName
            }).ToList();


            return Ok(ApiResponse.Succeeded(listOfPaletsVM, "Pallets obtenidos con éxito."));

        }


        // Este codigo llama las primeras 10 datos de la lista TOTAL de palets, haciendo paginado
        [HttpGet]
        [Route("api/ApiAccess/GetAllPalets")]
        public IActionResult getAllPalets(string searchTerm, int page = 1, int pageSize = 10)
        {

            var Palets = from s in _context.PaletsView select s;

           
            if (!string.IsNullOrEmpty(searchTerm))
            {
                Palets = from s in Palets
                         where s.UserFullName!.Contains(searchTerm) ||
                                  s.Descripcion!.Contains(searchTerm) ||
                                  s.DescEstado!.Contains(searchTerm)
                         select s;
            }


            var listOfPaletsVM = Palets
                .Skip((page - 1) * pageSize)  // Saltar los registros anteriores
                .Take(pageSize)  // Tomar solo el número de registros según pageSize
                .Select(x => new PaletVM()
                {
                    Id = x.Id,
                    Descripcion = x.Descripcion!,
                    FechaCreacion = x.FechaCreacion,
                    Observaciones = x.Observaciones,
                    Estado = x.Estado,
                    DescEstado = x.DescEstado,
                    ApplicationUserId = x.ApplicationUserId,
                    ApplicationUserName = x.UserFullName
                }).ToList();


            return Ok(ApiResponse.Succeeded(listOfPaletsVM, "Pallets obtenidos con éxito."));

        }



        // este codigo llama la lista total de todas las transferencias



        // Devuelve a la app una página usando la misma consulta y ordenamiento que la web.
        [HttpGet]
        [Route("api/ApiAccess/GetTransfers")]
        public async Task<IActionResult> GetTransfers(string userId, string searchTerm, int page = 1, int pageSize = 10)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (loggedInUser == null)
                return NotFound(ApiResponse.Failed("El usuario no existe.", "USER_NOT_FOUND"));

            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            // La API conserva su DTO y respuesta JSON; el servicio comparte solo la consulta.
            var result = await _transferenciaQueryService.GetPageAsync(new TransferenciaQuery
            {
                UserId = loggedInUser.Id,
                IsAdmin = loggedInUserRole.Contains(WebsiteRoles.Admin) ||
                          loggedInUserRole.Contains(WebsiteRoles.Supervisor),
                SearchTerm = searchTerm,
                Page = page,
                PageSize = pageSize
            }, includeTotalCount: false);

            var listOfTransferVM = result.Items.Select(x => new TransferenciaVM
                {
                    Id = x.Id,
                    CodigoInterno = x.CodigoInterno!,
                    FechaEnvio = x.FechaEnvio,
                    FechaRecibo = x.FechaRecibo,
                    FechaRechazo = x.FechaRechazo,
                    FechaAnulado = x.FechaAnulado,
                    Observaciones = x.Observaciones,
                    Estado = x.Estado,
                    DescEstado = x.DescEstado,
                    Foto = x.Foto,
                    IdUserEnvia = x.ApplicationUserIdEnvia!,
                    NombreUserEnvia = x.UserEnviaFullName!,
                    IdUserRecibe = x.ApplicationUserIdRecibe!,
                    NombreUserRecibe = x.UserRecibeFullName!,
                }).ToList();

            return Ok(ApiResponse.Succeeded(listOfTransferVM, "Transferencias obtenidas con éxito."));
        }




        [HttpGet]
        [Route("api/ApiAccess/GetTransferById")]
        public async Task<IActionResult> GetTransferById(int id)
        {

            var reg = await _context.TransferenciasView!.Where(x => x.Id == id).SingleOrDefaultAsync();
            if (reg == null)
            {
                return NotFound(ApiResponse.Failed("La transferencia no existe.", "TRANSFER_NOT_FOUND"));
            }
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
                                    Descripcion = palet.Descripcion!,
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
                DescEstado = reg.DescEstado,
                Estado = reg.Estado,
                FechaEnvio = reg.FechaEnvio,
                FechaRechazo = reg.FechaRechazo,
                FechaRecibo = reg.FechaRecibo,
                FechaAnulado = reg.FechaAnulado,
                IdUserEnvia = reg.ApplicationUserIdEnvia!,
                IdUserRecibe = reg.ApplicationUserIdRecibe!,
                NombreUserEnvia = reg.UserEnviaFullName!,
                NombreUserRecibe = reg.UserRecibeFullName!,
                NombreUsuariosResuelven = string.Join(", ", nombresUsuariosResuelven),
                Observaciones = reg.Observaciones,
                Foto = reg.Foto,
                Palets = palets
            };

           
            return Ok(ApiResponse.Succeeded(regVM, "Transferencia obtenida con éxito."));

        }



        [HttpGet]
        [Route("api/ApiAccess/GetTransferByPallet")]
        public async Task<IActionResult> GetTransferByPallet(int Id)
        {


            var listOfTransferVM = await (from transferencia in _context.TransferenciasView!.AsNoTracking()
                                          join detalle in _context.Detalles!.AsNoTracking()
                                              on transferencia.Id equals detalle.IdTransferencia
                                          where detalle.IdPalet == Id
                                          select new TransferenciaVM
                                          {
                                              Id = transferencia.Id,
                                              CodigoInterno = transferencia.CodigoInterno!,
                                              FechaEnvio = transferencia.FechaEnvio,
                                              FechaRecibo = transferencia.FechaRecibo,
                                              FechaRechazo = transferencia.FechaRechazo,
                                              FechaAnulado = transferencia.FechaAnulado,
                                              Observaciones = transferencia.Observaciones,
                                              Estado = transferencia.Estado,
                                              DescEstado = transferencia.DescEstado,
                                              Foto = transferencia.Foto,
                                              IdUserEnvia = transferencia.ApplicationUserIdEnvia!,
                                              NombreUserEnvia = transferencia.UserEnviaFullName!,
                                              IdUserRecibe = transferencia.ApplicationUserIdRecibe!,
                                              NombreUserRecibe = transferencia.UserRecibeFullName!
                                          })
                .Distinct()
                .OrderByDescending(x => x.FechaEnvio)
                .ToListAsync();


            return Ok(ApiResponse.Succeeded(listOfTransferVM, "Transferencias del pallet obtenidas con éxito."));

        }



        // Este codigo es de creacion de transferencia sin imagen es el original


        // Este codigo es de creacion de transferencia con almacenamiento local de imagenen en el mismo proyecto
        /*
        [HttpPost]
        [Route("api/ApiAccess/AddTransfer")]
        public async Task<ActionResult<string>> AddTransfer(TransferenciaVM regVM)
        {
            var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
            var defaultEstado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por recibir").SingleOrDefault();

            var reg = new Transferencia();

            reg.CodigoInterno = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            reg.FechaEnvio = DateTime.Now;
            reg.ApplicationUserIdEnvia = regVM.IdUserEnvia;
            reg.ApplicationUserIdRecibe = regVM.IdUserRecibe;
            reg.Estado = defaultEstado!.Id.ToString();

            // Lógica de carga de imagen
            if (regVM.FotoFile != null)
            {
                // Generar un nombre de archivo único
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + regVM.FotoFile.FileName;

                // Ruta donde se guardará la imagen
                var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "thumbnails");

                // Crear la carpeta si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Guardar el archivo en la ruta especificada
                var filePath = Path.Combine(folderPath, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await regVM.FotoFile.CopyToAsync(fileStream);
                }

                // Asignar el nombre del archivo al campo Foto en el modelo de transferencia
                reg.Foto = uniqueFileName;
            }

            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();

            var list_palets = JsonSerializer.Deserialize<List<int>>(regVM.JsonPalets!) ?? new List<int>();
            var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();

            foreach (var pal in list_palets)
            {
                var detalle = new Detalle
                {
                    IdPalet = pal,
                    IdTransferencia = reg.Id
                };
                await _context.Detalles!.AddAsync(detalle);

                var regPalet = await _context.Palets!.Where(x => x.Id == pal).SingleAsync();
                regPalet.Estado = newEstadoPalet.Id.ToString();
            }

            await _context.SaveChangesAsync();

            var regUserDestino = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserRecibe);
            var regUserEnvia = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserEnvia);

            if (regUserDestino == null || regUserEnvia == null)
                return BadRequest("Usuarios de envío o recepción no encontrados.");

            string fullNameUserDestino = regUserDestino.Nombres + " " + regUserDestino.Apellidos;

            await Utils.SendNotification(
                regUserDestino.FirebaseToken,
                regUserDestino.Email ?? string.Empty,
                fullNameUserDestino,
                "Has recibido una transferencia",
                "El usuario " + regUserEnvia.Nombres + " " + regUserEnvia.Apellidos + " te ha realizado la transferencia con codigo '" + reg.CodigoInterno + "'"
            );

            return CreatedAtAction("AddTransfer", "Ok, Transferencia se genero con exito");
        }
        */



        // Este codigo es de creacion de transferencia con almacenamiento de imagenenens en la nube  con FirebaseStorage
        // Este es el que esta en produccion 
        
        [HttpPost]
        [Route("api/ApiAccess/AddTransfer")]
        public async Task<IActionResult> AddTransfer(TransferenciaVM regVM)
        {
            var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
            var regUserDestino = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserRecibe);
            var regUserEnvia = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserEnvia);
            if (regUserDestino == null || regUserEnvia == null)
            {
                return NotFound(ApiResponse.Failed(
                    "El usuario de envío o recepción no existe.", "TRANSFER_USER_NOT_FOUND"));
            }
            var defaultEstado = listEstadosTrans.SingleOrDefault(x =>
                x.Descripcion!.ToLower() == "por recibir");
            if (defaultEstado == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failed(
                    "No está configurado el estado 'por recibir'.",
                    "TRANSFER_STATE_NOT_CONFIGURED"));
            }

            var reg = new Transferencia();

            reg.CodigoInterno = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
            reg.FechaEnvio = DateTime.UtcNow;
            reg.FechaLimiteAceptacion = DateTime.UtcNow.AddHours(48);
            reg.ApplicationUserIdEnvia = regVM.IdUserEnvia;
            reg.ApplicationUserIdRecibe = regVM.IdUserRecibe;
            reg.Estado = defaultEstado!.Id.ToString();

            // Lógica de carga de imagen en Firebase Storage
            if (regVM.FotoFile != null)
            {
                var firebaseStorageService = new FirebaseStorageService();

                using (var stream = regVM.FotoFile.OpenReadStream())
                {
                    // Subir a Firebase Storage y obtener la URL pública
                    reg.Foto = await firebaseStorageService.UploadImageAsync(stream, regVM.FotoFile.FileName);
                }
            }

            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();

            var list_palets = JsonSerializer.Deserialize<List<int>>(regVM.JsonPalets!) ?? new List<int>();
            var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();
            var estadoDetallePendiente = await _detalleTransferenciaService
                .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);

            var idsPalets = list_palets.Distinct().ToList();
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

            string fullNameUserDestino = regUserDestino.Nombres + " " + regUserDestino.Apellidos;

            await Utils.SendNotification(
                    regUserDestino.FirebaseToken,
                    regUserDestino.Email ?? string.Empty,
                    fullNameUserDestino,
                    "Has recibido una transferencia",
                    "El usuario " + regUserEnvia.Nombres + " " + regUserEnvia.Apellidos + " te ha realizado la transferencia con codigo '" + reg.CodigoInterno + "'"
                );

            return CreatedAtAction("AddTransfer", ApiResponse.Succeeded(
                new { transferenciaId = reg.Id, fechaLimiteAceptacion = reg.FechaLimiteAceptacion },
                "Transferencia creada con éxito."));
        }



        [HttpPost]
        [Route("api/ApiAccess/AddReclamo")]
        public async Task<IActionResult> AddReclamo(TransferenciaVM regVM)
        {
            var usuario = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == regVM.IdUserEnvia);
            if (usuario == null)
            {
                return NotFound(ApiResponse.Failed(
                    "El usuario que crea el reclamo no existe.", "USER_NOT_FOUND"));
            }

            var rolesPermitidos = new[] { WebsiteRoles.Admin, WebsiteRoles.Bodeguero, WebsiteRoles.Chofer };
            var rolesUsuario = await _userManager.GetRolesAsync(usuario);
            if (!rolesUsuario.Any(x => rolesPermitidos.Contains(x)))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failed(
                    "El usuario no tiene permiso para crear reclamos.", "CLAIM_FORBIDDEN"));
            }

            List<int> idsPalets;
            try
            {
                idsPalets = (JsonSerializer.Deserialize<List<int>>(regVM.JsonPalets ?? "[]") ?? new List<int>())
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();
            }
            catch (JsonException)
            {
                return BadRequest(ApiResponse.Failed(
                    "La lista de pallets no tiene un formato válido.", "INVALID_PALLET_LIST"));
            }

            if (idsPalets.Count == 0)
            {
                return BadRequest(ApiResponse.Failed(
                    "Debe seleccionar por lo menos un pallet.", "PALLETS_REQUIRED"));
            }

            var estados = await _context.Catalogos!
                .Where(x => (x.Categoria == "estado_transferencia" &&
                             x.Descripcion!.ToLower() == "por reclamar") ||
                            (x.Categoria == "estado_palets" &&
                             x.Descripcion!.ToLower() == "dado de baja"))
                .ToListAsync();
            var estadoPorReclamar = estados.SingleOrDefault(x =>
                x.Categoria == "estado_transferencia" && x.Descripcion!.ToLower() == "por reclamar");
            var estadoDadoBaja = estados.SingleOrDefault(x =>
                x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "dado de baja");
            if (estadoPorReclamar == null || estadoDadoBaja == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failed(
                    "No están configurados los estados necesarios para crear el reclamo.",
                    "CLAIM_STATES_NOT_CONFIGURED"));
            }

            // La validación y el cambio de estado deben ser atómicos para impedir reclamos simultáneos.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
            if (palets.Count != idsPalets.Count)
            {
                return NotFound(ApiResponse.Failed(
                    "Uno o más pallets no existen.", "PALLET_NOT_FOUND"));
            }

            // Cualquier estado operativo admite reclamos; solo se excluyen pallets propios o dados de baja.
            var noDisponibles = palets.Where(x =>
                x.ApplicationUserId == usuario.Id ||
                x.Estado == estadoDadoBaja.Id.ToString()).ToList();
            if (noDisponibles.Count > 0)
            {
                var idsResponsables = noDisponibles.Select(x => x.ApplicationUserId).Where(x => x != null).ToList();
                var responsables = await _userManager.Users
                    .Where(x => idsResponsables.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => (x.Nombres + " " + x.Apellidos).Trim());
                var detalle = string.Join(", ", noDisponibles.Select(x =>
                    $"{x.Descripcion} ({(responsables.TryGetValue(x.ApplicationUserId ?? string.Empty, out var nombre) ? nombre : "responsable no identificado")})"));
                return Conflict(ApiResponse.Failed(
                    $"No puedes reclamar pallets propios o dados de baja: {detalle}.",
                    "PALLET_NOT_AVAILABLE"));
            }

            var estadoDetallePendiente = await _detalleTransferenciaService
                .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
            var reclamosDuplicados = await (from detalle in _context.Detalles!
                                        join transferencia in _context.Transferencias!
                                            on detalle.IdTransferencia equals transferencia.Id
                                        where idsPalets.Contains(detalle.IdPalet) &&
                                              (detalle.Estado == estadoDetallePendiente || detalle.Estado == string.Empty) &&
                                              transferencia.ApplicationUserIdEnvia == usuario.Id &&
                                              transferencia.ApplicationUserIdRecibe == "Administradores"
                                        select detalle.IdPalet).Distinct().ToListAsync();
            if (reclamosDuplicados.Count > 0)
            {
                return Conflict(ApiResponse.Failed(
                    "Ya tienes un reclamo pendiente para uno o más pallets seleccionados.",
                    "DUPLICATE_PENDING_CLAIM"));
            }

            var reg = new Transferencia
            {
                CodigoInterno = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss"),
                FechaEnvio = DateTime.UtcNow,
                ApplicationUserIdEnvia = usuario.Id,
                ApplicationUserIdRecibe = "Administradores",
                Estado = estadoPorReclamar.Id.ToString(),
                Observaciones = regVM.Observaciones
            };

            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();
            await _context.Detalles!.AddRangeAsync(palets.Select(x => new Detalle
            {
                IdPalet = x.Id,
                IdTransferencia = reg.Id,
                Estado = estadoDetallePendiente,
                FechaEstado = DateTime.UtcNow,
                EstadoPaletAnterior = x.Estado,
                ApplicationUserIdCustodioAnterior = x.ApplicationUserId
            }));
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _reclamoService.NotificarSupervisoresAsync(usuario, reg.Id);

            return CreatedAtAction("AddReclamo", ApiResponse.Succeeded(
                new { transferenciaId = reg.Id },
                "Reclamo creado con éxito."));
        }








        //Tiene una version de las fechas que aun no esta probada ya que, internamanete ya esta modificado, habria que probar aun








        [HttpGet]
        [Route("api/ApiAccess/notifications/token")]
        public async Task<IActionResult> GetFirebaseAccessToken()
        {
            try
            {
                string token = await Utils.GetAccessToken();
                return Ok(ApiResponse.Succeeded(new { accessToken = token }, "Token obtenido con éxito."));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Failed("Error al obtener el token de Firebase.", "FIREBASE_TOKEN_ERROR"));
            }
        }


        // PROCESO CON RECLAMO
        [HttpPost]
        [Route("api/ApiAccess/ProcessTransfer")]
        [Consumes("application/json")]
        public async Task<IActionResult> ProcessTransfer([FromBody] ImageTransferModel imageTransferModel)
        {

            try
            {
                // Codigo para recibir imagene cuando procesen la transferencia

                var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();

                string operacion = imageTransferModel.Estado;

                var regTrans = await _context.Transferencias!
                    .SingleOrDefaultAsync(x => x.Id == imageTransferModel.TransferenciaId);
                if (regTrans == null)
                {
                    return NotFound(ApiResponse.Failed(
                        "La transferencia no existe.", "TRANSFER_NOT_FOUND"));
                }

                regTrans!.Observaciones = imageTransferModel.Observaciones;

                // Verificar si la transferencia ya está anulada
                var estadoAnulado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "anulado")?.Id.ToString();
                var estadoPorReclamar = listEstadosTrans.Single(x => x.Descripcion!.ToLower() == "por reclamar").Id.ToString();
                var esReclamo = regTrans.Estado == estadoPorReclamar;
                var usuarioProcesa = await _userManager.Users
                    .SingleOrDefaultAsync(x => x.Id == imageTransferModel.IdUserProcesa);
                if (usuarioProcesa == null)
                {
                    return Unauthorized(ApiResponse.Failed(
                        "No se pudo identificar al usuario que procesa la transferencia.",
                        "PROCESSING_USER_REQUIRED"));
                }
                var esAdmin = await _userManager.IsInRoleAsync(usuarioProcesa, WebsiteRoles.Admin!) ||
                              await _userManager.IsInRoleAsync(usuarioProcesa, WebsiteRoles.Supervisor!);
                var puedeProcesar = esReclamo
                    ? esAdmin
                    : regTrans.ApplicationUserIdRecibe == usuarioProcesa.Id;
                if (!puedeProcesar)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failed(
                        "El usuario no tiene permiso para procesar esta transferencia.",
                        "TRANSFER_PROCESS_FORBIDDEN"));
                }
                if (regTrans.Estado == estadoAnulado)
                {
                    // Salir de la operación si la transferencia ya fue anulada
                    return Conflict(ApiResponse.Failed(
                        "La transferencia ya fue anulada y no puede procesarse.",
                        "TRANSFER_ALREADY_CANCELLED"));
                }

                if (operacion != "aceptar" && operacion != "rechazar")
                {
                    return BadRequest(ApiResponse.Failed(
                        "La operación debe ser aceptar o rechazar.", "INVALID_TRANSFER_OPERATION"));
                }
                if (operacion == "aceptar")
                {
                    if (esReclamo)
                    {
                        await _reclamoService.AdjudicarAsync(
                            imageTransferModel.TransferenciaId,
                            usuarioProcesa.Id,
                            imageTransferModel.Observaciones);

                    }
                    else
                    {
                        // Procesamiento normal para transferencias
                        // Obtener todos los detalles de la transferencia
                        var detalles = await _detalleTransferenciaService
                            .GetPendientesAsync(imageTransferModel.TransferenciaId);
                        var idsPalets = detalles.Select(x => x.IdPalet).ToList();
                        var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
                        var newEstadoPalet = await _context.Catalogos!
                            .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                            .SingleAsync();
                        var estadoDetalle = await _detalleTransferenciaService
                            .GetEstadoIdAsync(DetalleTransferenciaEstados.Recibido);

                        foreach (var regPalet in palets)
                        {
                            // Una transferencia aceptada inicia una custodia nueva, aunque antes estuviera reclamado.
                            regPalet.Estado = newEstadoPalet.Id.ToString();
                            regPalet.ApplicationUserId = regTrans.ApplicationUserIdRecibe;

                        }
                        DetalleTransferenciaService.CambiarEstado(
                            detalles,
                            estadoDetalle,
                            usuarioProcesa.Id,
                            imageTransferModel.Observaciones);
                        await _detalleTransferenciaService.RecalcularCabecerasAsync(new[] { regTrans.Id });

                    }

                }
                else if (operacion == "rechazar")
                {
                    regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
                    regTrans.FechaRechazo = DateTime.UtcNow;

                    var detalles = await _detalleTransferenciaService
                        .GetPendientesAsync(imageTransferModel.TransferenciaId);

                    var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();
                    var estadoPaletEnTransferencia = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();
                    var estadoDetalle = await _detalleTransferenciaService
                        .GetEstadoIdAsync(DetalleTransferenciaEstados.Rechazado);
                    var idsPalets = detalles.Select(x => x.IdPalet).ToList();
                    var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
                    var idsOrigen = detalles.Where(x => x.IdDetalleOrigen.HasValue)
                        .Select(x => x.IdDetalleOrigen!.Value).ToList();
                    var detallesOrigen = await _context.Detalles!
                        .Where(x => idsOrigen.Contains(x.Id)).ToListAsync();
                    var palletsConOrigen = detalles.Where(x => x.IdDetalleOrigen.HasValue)
                        .Select(x => x.IdPalet).ToHashSet();

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
                        detalles,
                        estadoDetalle,
                        usuarioProcesa.Id,
                        imageTransferModel.Observaciones);
                    if (!esReclamo && detallesOrigen.Count > 0)
                    {
                        var estadoPendiente = await _detalleTransferenciaService
                            .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
                        DetalleTransferenciaService.CambiarEstado(
                            detallesOrigen, estadoPendiente, usuarioProcesa.Id, imageTransferModel.Observaciones);
                        await _detalleTransferenciaService.RecalcularCabecerasAsync(
                            detallesOrigen.Select(x => x.IdTransferencia));
                    }
                    if (!esReclamo)
                    {
                        await _detalleTransferenciaService.RecalcularCabecerasAsync(new[] { regTrans.Id });
                    }

                }
                // Esta linea es para almacenar el nombre de la imagen en el campo Foto de la transfeencia
                //regTrans.Foto = uniqueFileName;
                await _context.SaveChangesAsync();







                // despues de actualizar el estado de la transferencia se envian las notificaciones correspondientes
                var viewTrans = await _context.TransferenciasView!.Where(x => x.Id == imageTransferModel.TransferenciaId).SingleAsync();

                if (operacion == "aceptar")
                {
                    await Utils.SendNotification(viewTrans.UserEnviaFirebaseToken,
                                           viewTrans.UserEnviaEmail ?? string.Empty,
                                           viewTrans.UserEnviaFullName ?? string.Empty,
                                           "Transferencia aceptada",
                                           "El usuario " + viewTrans.UserRecibeFullName +
                                           " ha aceptado la transferencia con codigo '" +
                                           viewTrans.CodigoInterno + "'");



                }
                else if (operacion == "rechazar")
                {
                    await Utils.SendNotification(viewTrans.UserEnviaFirebaseToken,
                                           viewTrans.UserEnviaEmail ?? string.Empty,
                                           viewTrans.UserEnviaFullName ?? string.Empty,
                                           "Transferencia rechazada",
                                           "El usuario " + viewTrans.UserRecibeFullName +
                                           " ha rechazado la transferencia con codigo '" +
                                           viewTrans.CodigoInterno + "'");
                }


                var message = operacion == "aceptar"
                    ? "Transferencia aceptada con éxito."
                    : "Transferencia rechazada con éxito.";
                return Ok(ApiResponse.Succeeded(null, message));

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failed(
                    "Ocurrió un error inesperado al procesar la transferencia.",
                    "TRANSFER_PROCESSING_ERROR"));
            }


        }

        //PROCESA LA TRANSFERENCIA CON IMAGEN CUANDO EL USUARIO RECIBE [ORIGINAL]   NO RECLAMOS   





        [HttpPost]
        [Route("api/ApiAccess/AnularTransfer")]
        public async Task<IActionResult> AnularTransfer([FromBody] ImageTransferModel imageTransferModel)
        {

            try
            {
                var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
                
                // Obtener el registro de la transferencia
                var regTrans = await _context.Transferencias!
                    .SingleOrDefaultAsync(x => x.Id == imageTransferModel.TransferenciaId);
                if (regTrans == null)
                {
                    return NotFound(ApiResponse.Failed(
                        "La transferencia no existe.", "TRANSFER_NOT_FOUND"));
                }

                // Verificar si el estado es "aceptado" o "rechazado"
                //var estadoAnulado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "anulado")?.Id.ToString();
                var estadoAceptado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "recibido")?.Id.ToString();
                var estadoRechazado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "rechazado")?.Id.ToString();
                var estadoPorReclamar = listEstadosTrans.Single(x => x.Descripcion!.ToLower() == "por reclamar").Id.ToString();
                var esReclamo = regTrans.Estado == estadoPorReclamar;
                var usuarioProcesa = await _userManager.Users
                    .SingleOrDefaultAsync(x => x.Id == imageTransferModel.IdUserProcesa);
                if (usuarioProcesa == null)
                {
                    return Unauthorized(ApiResponse.Failed(
                        "No se pudo identificar al usuario que anula la transferencia.",
                        "PROCESSING_USER_REQUIRED"));
                }
                var esAdmin = await _userManager.IsInRoleAsync(usuarioProcesa, WebsiteRoles.Admin!) ||
                              await _userManager.IsInRoleAsync(usuarioProcesa, WebsiteRoles.Supervisor!);
                var puedeAnular = esReclamo
                    ? regTrans.ApplicationUserIdEnvia == usuarioProcesa.Id
                    : regTrans.ApplicationUserIdEnvia == usuarioProcesa.Id || esAdmin;
                if (!puedeAnular)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failed(
                        "El usuario no tiene permiso para anular esta transferencia.",
                        "TRANSFER_CANCEL_FORBIDDEN"));
                }

                if (regTrans.Estado == estadoAceptado || regTrans.Estado == estadoRechazado)
                {
                    return Conflict(ApiResponse.Failed(
                        "La transferencia ya fue procesada y no puede anularse.",
                        "TRANSFER_ALREADY_PROCESSED"));
                }

                // Actualizar el estado a "anulado" y agregar la observación
                regTrans!.Observaciones = imageTransferModel.Observaciones;
                regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
                regTrans.FechaAnulado = DateTime.UtcNow;

                // Actualizar el estado de los pallets asociados a "disponible"
                var detalles = await _detalleTransferenciaService
                    .GetPendientesAsync(imageTransferModel.TransferenciaId);

                var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();
                var estadoPaletEnTransferencia = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();
                var estadoDetalle = await _detalleTransferenciaService
                    .GetEstadoIdAsync(DetalleTransferenciaEstados.Anulado);
                var idsPalets = detalles.Select(x => x.IdPalet).ToList();
                var palets = await _context.Palets!.Where(x => idsPalets.Contains(x.Id)).ToListAsync();
                var idsOrigen = detalles.Where(x => x.IdDetalleOrigen.HasValue)
                    .Select(x => x.IdDetalleOrigen!.Value).ToList();
                var detallesOrigen = await _context.Detalles!
                    .Where(x => idsOrigen.Contains(x.Id)).ToListAsync();
                var palletsConOrigen = detalles.Where(x => x.IdDetalleOrigen.HasValue)
                    .Select(x => x.IdPalet).ToHashSet();

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
                    detalles,
                    estadoDetalle,
                    usuarioProcesa.Id,
                    imageTransferModel.Observaciones);
                if (!esReclamo && detallesOrigen.Count > 0)
                {
                    var estadoPendiente = await _detalleTransferenciaService
                        .GetEstadoIdAsync(DetalleTransferenciaEstados.Pendiente);
                    DetalleTransferenciaService.CambiarEstado(
                        detallesOrigen, estadoPendiente, usuarioProcesa.Id, imageTransferModel.Observaciones);
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(
                        detallesOrigen.Select(x => x.IdTransferencia));
                }
                if (!esReclamo)
                {
                    await _detalleTransferenciaService.RecalcularCabecerasAsync(new[] { regTrans.Id });
                }

                await _context.SaveChangesAsync();


                return Ok(ApiResponse.Succeeded(null, "Transferencia anulada con éxito."));

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failed(
                    "Ocurrió un error inesperado al anular la transferencia.",
                    "TRANSFER_CANCELLATION_ERROR"));
            }


        }



        [HttpGet]
        [Route("api/ApiAccess/GetCatalogoByCategory")]
        public async Task<IActionResult> GetCatalogoByCategory(string cat)
        {

            List<CatalogoVM> lista = new List<CatalogoVM>();

            try
            {

                var listado = from reg in _context.Catalogos
                               where reg.Categoria == cat
                               select new CatalogoVM
                               {

                                   Id = reg.Id,
                                   Categoria = reg.Categoria ?? string.Empty,
                                   Descripcion = reg.Descripcion ?? string.Empty

                               };

                lista = await listado.ToListAsync();

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Failed("Ocurrió un error al consultar el catálogo.", "CATALOG_QUERY_ERROR"));
            }

            return Ok(ApiResponse.Succeeded(lista, "Catálogo obtenido con éxito."));

        }

        [HttpGet]
        [Route("api/ApiAccess/GetRoles")]
        public async Task<IActionResult> GetRoles()
        {

            List<RolVM> lista = new List<RolVM>();

            try
            {

                var lstRoles = from de in _roleManager.Roles
                               select new RolVM
                               {
                                   Id = de.Id,
                                   Nombre = de.Name ?? string.Empty,
                               };

                lista = await lstRoles.ToListAsync();

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Failed("Ocurrió un error al consultar los roles.", "ROLES_QUERY_ERROR"));
            }

            return Ok(ApiResponse.Succeeded(lista, "Roles obtenidos con éxito."));

        }

        [HttpPost]
        [Route("api/ApiAccess/AddClient")]
        public async Task<IActionResult> AddClient([FromBody] RegisterUserVM regVM)
        {

            var checkUserByEmail = await _userManager.FindByEmailAsync(regVM.Email);
            if (checkUserByEmail != null)
            {
                return Conflict(ApiResponse.Failed("El correo electrónico ya existe.", "EMAIL_ALREADY_EXISTS"));
            }
            var checkUserByUsername = await _userManager.FindByNameAsync(regVM.UserName);
            if (checkUserByUsername != null)
            {
                return Conflict(ApiResponse.Failed("El nombre de usuario ya existe.", "USERNAME_ALREADY_EXISTS"));
                
            }

            if (regVM.Documento!.Length >= 10)
            {
                var ct = _context.UsersView!.Count(x => regVM.Documento!.StartsWith(x.Documento!));
                if (ct > 0) {
                    return Conflict(ApiResponse.Failed("El documento ya está registrado.", "DOCUMENT_ALREADY_EXISTS"));
                }
            }


            var applicationUser = new ApplicationUser()
            {
                Email = regVM.Email,
                UserName = regVM.UserName,
                Nombres = regVM.Nombres,
                Apellidos = regVM.Apellidos,
                TipoDocumento = regVM.TipoDocumento,
                Documento = regVM.Documento,
                RazonSocial = regVM.RazonSocial,
                Activo = false
            };

            var result = await _userManager.CreateAsync(applicationUser, regVM.Password);
            if (result.Succeeded)
            {

                var newRegRol = (from de in _roleManager.Roles
                                 where de.Id == regVM.Rol
                                 select de).FirstOrDefault();


                if (newRegRol?.Name == null)
                    return NotFound(ApiResponse.Failed("El rol no existe.", "ROLE_NOT_FOUND"));

                await _userManager.AddToRoleAsync(applicationUser, newRegRol.Name);

               
            }
            else
            {
                var message = string.Join(" ", result.Errors.Select(x => x.Description));
                return BadRequest(ApiResponse.Failed(
                    string.IsNullOrWhiteSpace(message) ? "No se pudo registrar el usuario." : message,
                    "USER_CREATION_FAILED"));
            }
            
            return CreatedAtAction("AddClient", ApiResponse.Succeeded(
                new { userId = applicationUser.Id }, "Usuario registrado con éxito."));
        }


        [HttpPost]
        [Route("api/ApiAccess/ChangePasswordClient")]
        public async Task<IActionResult> ChangePasswordClient([FromBody] RegisterUserVM regVM)
        {

            var checkUserByUsername = await _userManager.FindByNameAsync(regVM.UserName);
            if (checkUserByUsername == null)
            {
                return NotFound(ApiResponse.Failed("El usuario no existe.", "USER_NOT_FOUND"));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(checkUserByUsername);
            var result = await _userManager.ResetPasswordAsync(checkUserByUsername, token, regVM.Password);
            if (!result.Succeeded)
            {
                var message = string.Join(" ", result.Errors.Select(x => x.Description));
                return BadRequest(ApiResponse.Failed(
                    string.IsNullOrWhiteSpace(message) ? "No se pudo cambiar la contraseña." : message,
                    "PASSWORD_CHANGE_FAILED"));
            }

            return Ok(ApiResponse.Succeeded(null, "Contraseña cambiada con éxito."));
            
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}





