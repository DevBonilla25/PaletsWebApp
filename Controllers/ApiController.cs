using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;
using PaletsWebApp.ViewModels;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaletsWebApp.Controllers
{
    public class ApiController : Controller
    {
        private readonly ILogger<ApiController> _logger;
        private readonly ApplicationDbContext _context;
        public INotyfService _notification { get; }
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ApiController(ILogger<ApiController> logger,
                             ApplicationDbContext context,
                             INotyfService notyfService,
                             IWebHostEnvironment webHostEnvironment,
                             UserManager<ApplicationUser> userManager,
                             RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _notification = notyfService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [HttpGet]
        [Route("api/ApiAccess/Login")]
        public async Task<ActionResult<RegisterUserVM>> Login(string user, 
                                                              string password, 
                                                              string firebaseToken)
        {

            RegisterUserVM vm = new RegisterUserVM();

            try
            {

                var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == user);

                if (regUser == null)
                {
                    return Problem("Usuario no existe.");
                }

                if (regUser != null)
                {

                    if (regUser.Activo == false)
                    {
                        return Problem("Cuenta no se ha activado aun");
                    }

                    var verifyPassword = await _userManager.CheckPasswordAsync(regUser, password);
                    if (!verifyPassword)
                    {
                        return Problem("Contraseña invalida");
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
                    return Problem("Usuario no existe.");


            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            return CreatedAtAction("Login", new { id =vm.Id }, vm);

        }

        // Devuelve la lusta de todos los usuarios
        [HttpGet]
        [Route("api/ApiAccess/GetUsers")]
        public async Task<ActionResult<RegisterUserVM>> GetUsers(string userId)
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
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            return CreatedAtAction("GetUsers", lista);

        }



        // Esta consulta devuelve a un usuario enviado por parametro Id
        [HttpGet]
        [Route("api/ApiAccess/GetUserById")]
        public async Task<ActionResult<RegisterUserVM>> GetUserById(string userId)
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
                    return NotFound($"Usuario con id {userId} no encontrado.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            // Retornar el usuario encontrado
            return Ok(usuario);
        }



        // este codigo llama la lista total de todos los palets
        /*
        [HttpGet]
        [Route("api/ApiAccess/GetPalets")]
        public async Task<ActionResult<List<PaletVM>>> getPalets(string userId, string searchTerm)
        {

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            
            if(loggedInUser == null)
                return Problem("Usuario no existe");

            var Palets = from s in _context.PaletsView select s;

            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if (loggedInUserRole[0] != WebsiteRoles.Admin)
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


            var listOfPaletsVM = Palets.Select(x => new PaletVM()
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


            return CreatedAtAction("GetPalets", listOfPaletsVM);

        }
        */

        // Este codigo llama las primeras 10 datos de palets por Usario, haciendo paginado
        [HttpGet]
        [Route("api/ApiAccess/GetPaletsByUser")]
        public async Task<ActionResult<List<PaletVM>>> getPaletsByUserId(string userId, string searchTerm, int page = 1, int pageSize = 10)
        {

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (loggedInUser == null)
                return Problem("Usuario no existe");

            var Palets = from s in _context.PaletsView select s;

            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if (loggedInUserRole[0] != WebsiteRoles.Admin)
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


            return CreatedAtAction("GetPalets", listOfPaletsVM);

        }


        // Este codigo llama las primeras 10 datos de la lista TOTAL de palets, haciendo paginado
        [HttpGet]
        [Route("api/ApiAccess/GetAllPalets")]
        public ActionResult<List<PaletVM>> getAllPalets(string searchTerm, int page = 1, int pageSize = 10)
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


            return CreatedAtAction("GetPalets", listOfPaletsVM);

        }



        // este codigo llama la lista total de todas las transferencias
        /*
        [HttpGet]
        [Route("api/ApiAccess/GetTransfers")]
        public async Task<ActionResult<List<TransferenciaVM>>> GetTransfers(string userId, string searchTerm)
        {

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (loggedInUser == null)
                return Problem("Usuario no existe");

            var Transfers = from s in _context.TransferenciasView select s;

            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if (loggedInUserRole[0] != WebsiteRoles.Admin)
            {
                Transfers = from s in Transfers
                            where s.ApplicationUserIdEnvia == loggedInUser!.Id ||
                                  s.ApplicationUserIdRecibe == loggedInUser!.Id
                                 select s;
            }


            if (!string.IsNullOrEmpty(searchTerm))
            {

               
                Transfers = from s in Transfers
                            where s.UserEnviaFullName!.Contains(searchTerm) ||
                                  s.UserRecibeFullName!.Contains(searchTerm) ||
                                  s.DescEstado!.Contains(searchTerm)  
                            select s;
            }
                        


            if (Transfers.Count() == 0)
            {
                return CreatedAtAction("GetTransfers", new List<TransferenciaVM>());
            }


            var listOfTransferVM = Transfers.Select(x => new TransferenciaVM()
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


            return CreatedAtAction("GetTransfers", listOfTransferVM);

        }
        */



        // Este codigo llama las primeras 10 datos de la transferencia haciendo paginado
        [HttpGet]
        [Route("api/ApiAccess/GetTransfers")]
        public async Task<ActionResult<List<TransferenciaVM>>> GetTransfers(string userId, string searchTerm, int page = 1, int pageSize = 10)
        {
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (loggedInUser == null)
                return Problem("Usuario no existe");

            var Transfers = from s in _context.TransferenciasView select s;

            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if (loggedInUserRole[0] != WebsiteRoles.Admin)
            {
                Transfers = from s in Transfers
                            where s.ApplicationUserIdEnvia == loggedInUser!.Id ||
                                  s.ApplicationUserIdRecibe == loggedInUser!.Id
                            select s;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                Transfers = from s in Transfers
                            where s.UserEnviaFullName!.Contains(searchTerm) ||
                                  s.UserRecibeFullName!.Contains(searchTerm) ||
                                  s.DescEstado!.Contains(searchTerm)
                            select s;
            }

            var listOfTransferVM = Transfers
                .OrderByDescending(x => x.FechaEnvio)
                .Skip((page - 1) * pageSize)  // Saltar los registros anteriores
                .Take(pageSize)  // Tomar solo el número de registros según pageSize
                .Select(x => new TransferenciaVM()
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

            return CreatedAtAction("GetTransfers", listOfTransferVM);
        }




        [HttpGet]
        [Route("api/ApiAccess/GetTransferById")]
        public async Task<ActionResult<TransferenciaVM>> GetTransferById(int id)
        {

            var reg = await _context.TransferenciasView!.Where(x => x.Id == id).SingleOrDefaultAsync();
            var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == id).ToListAsync();

            var ll = from dr in detalles
                     from dp in _context.Palets!
                     where dr.IdPalet == dp.Id
                     select new PaletVM
                     {
                         Id = dp.Id,
                         Descripcion = dp.Descripcion!,
                     };


            var regVM = new TransferenciaVM
            {
                Id = reg!.Id,
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
                Observaciones = reg.Observaciones,
                Foto = reg.Foto,
                Palets = ll.ToList()
            };

           
            return CreatedAtAction("GetTransferById", regVM);

        }



        [HttpGet]
        [Route("api/ApiAccess/GetTransferByPallet")]
        public async Task<ActionResult<TransferenciaVM>> GetTransferByPallet(int Id)
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


            return CreatedAtAction("GetTransferByPallet", listOfTransferVM);

        }



        // Este codigo es de creacion de transferencia sin imagen es el original
        /*
        [HttpPost]
        [Route("api/ApiAccess/AddTransfer")]
        public async Task<ActionResult<string>> AddTransfer([FromBody] TransferenciaVM regVM)
        {

            var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
            var defaultEstado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por recibir").SingleOrDefault();

            var reg = new Transferencia();

            reg.CodigoInterno = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            reg.FechaEnvio = DateTime.Now;
            reg.ApplicationUserIdEnvia = regVM.IdUserEnvia;
            reg.ApplicationUserIdRecibe = regVM.IdUserRecibe;
            reg.Estado = defaultEstado!.Id.ToString();

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


            await Utils.SendNotification(regUserDestino.FirebaseToken,
                                   regUserDestino.Email ?? string.Empty,
                                   fullNameUserDestino,
                                   "Has recibido una transferencia ",
                                   "El usuario " + regUserEnvia.Nombres + " " + regUserEnvia.Apellidos + " te ha realizado la transferencia con codigo '" + reg.CodigoInterno + "'");


            return CreatedAtAction("AddTransfer", "Ok, Transferencia se genero con exito");
        }
        */


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
        public async Task<ActionResult<string>> AddTransfer(TransferenciaVM regVM)
        {
            var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
            var defaultEstado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por recibir").SingleOrDefault();

            var reg = new Transferencia();

            reg.CodigoInterno = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
            reg.FechaEnvio = DateTime.UtcNow;
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



        [HttpPost]
        [Route("api/ApiAccess/AddReclamo")]
        public async Task<ActionResult<string>> AddReclamo(TransferenciaVM regVM)
        {
            var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
            var defaultEstado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault();

            var reg = new Transferencia();

            reg.CodigoInterno = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
            reg.FechaEnvio = DateTime.UtcNow;
            reg.ApplicationUserIdEnvia = regVM.IdUserEnvia;
            reg.ApplicationUserIdRecibe = "Administradores"; ;
            reg.Estado = defaultEstado!.Id.ToString();

           
            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();


            var list_palets = JsonSerializer.Deserialize<List<int>>(regVM.JsonPalets!) ?? new List<int>();
            var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en reclamo").SingleAsync();

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
            var regUserEnvia = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserEnvia);

            return CreatedAtAction("AddReclamo", new { message = "Reclamo creado con éxito.", transferenciaId = reg.Id });
        }








        //Tiene una version de las fechas que aun no esta probada ya que, internamanete ya esta modificado, habria que probar aun
        /*
        [HttpPost]
        [Route("api/ApiAccess/AddTransfer")]
        public async Task<ActionResult<string>> AddTransfer(TransferenciaVM regVM)
        {
            // Verificación de campos obligatorios en TransferenciaVM
            if (string.IsNullOrWhiteSpace(regVM.IdUserEnvia) ||
                string.IsNullOrWhiteSpace(regVM.IdUserRecibe) ||
                string.IsNullOrWhiteSpace(regVM.JsonPalets))
            {
                return BadRequest("Campos obligatorios faltantes o vacíos.");
            }

            var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
            var defaultEstado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "por recibir");

            if (defaultEstado == null)
            {
                return BadRequest("Estado predeterminado no encontrado en la base de datos.");
            }

            // Ajuste de la hora a la zona horaria de Ecuador (UTC-5)
            TimeZoneInfo zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            DateTime fechaEnvioLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaHoraria);

            // Crear el objeto Transferencia con datos validados
            var reg = new Transferencia
            {
                CodigoInterno = fechaEnvioLocal.ToString("yyyy_MM_dd_HH_mm_ss"),
                FechaEnvio = fechaEnvioLocal, // Hora ajustada a la zona horaria
                ApplicationUserIdEnvia = regVM.IdUserEnvia,
                ApplicationUserIdRecibe = regVM.IdUserRecibe,
                Estado = defaultEstado.Id.ToString()
            };

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

            // Guardar Transferencia en la base de datos
            await _context.Transferencias!.AddAsync(reg);
            await _context.SaveChangesAsync();

            // Deserializar y validar la lista de palets
            var list_palets = JsonSerializer.Deserialize<List<int>>(regVM.JsonPalets!) ?? new List<int>();
            if (list_palets == null || !list_palets.Any())
            {
                return BadRequest("Lista de palets no válida o vacía.");
            }

            var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "en transferencia").SingleAsync();

            // Crear detalles de palets y actualizar estados
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

            // Notificar al usuario receptor
            var regUserDestino = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserRecibe);
            var regUserEnvia = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == regVM.IdUserEnvia);

            if (regUserDestino == null || regUserEnvia == null)
            {
                return BadRequest("Usuarios de envío o recepción no encontrados.");
            }

            string fullNameUserDestino = $"{regUserDestino.Nombres} {regUserDestino.Apellidos}";
            await Utils.SendNotification(
                regUserDestino.FirebaseToken,
                regUserDestino.Email ?? string.Empty,
                fullNameUserDestino,
                "Has recibido una transferencia",
                $"El usuario {regUserEnvia.Nombres} {regUserEnvia.Apellidos} te ha realizado la transferencia con código '{reg.CodigoInterno}'"
            );

            return CreatedAtAction("AddTransfer", "Ok, Transferencia se generó con éxito");
        }
        */








        [HttpGet]
        [Route("api/ApiAccess/notifications/token")]
        public async Task<IActionResult> GetFirebaseAccessToken()
        {
            try
            {
                string token = await Utils.GetAccessToken();
                return Ok(new { accessToken = token });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el token de Firebase");
            }
        }


        // PROCESO CON RECLAMO
        [HttpPost]
        [Route("api/ApiAccess/ProcessTransfer")]
        public async Task<IActionResult> ProcessTransfer([FromBody] ImageTransferModel imageTransferModel)
        {

            try
            {
                // Codigo para recibir imagene cuando procesen la transferencia
                /*
                string uniqueFileName = "";

                if (imageTransferModel.ImageArray != null)
                {
                    var stream = new MemoryStream(imageTransferModel.ImageArray);

                    var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "thumbnails");
                    uniqueFileName = Guid.NewGuid().ToString() + ".jpg";
                    var filePath = Path.Combine(folderPath, uniqueFileName);
                    using (FileStream fileStream = System.IO.File.Create(filePath))
                    {
                        stream.CopyTo(fileStream);
                    }

                }
                */

                var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();

                string operacion = imageTransferModel.Estado;

                var regTrans = await _context.Transferencias!.Where(x => x.Id == imageTransferModel.TransferenciaId).SingleAsync();
                regTrans!.Observaciones = imageTransferModel.Observaciones;

                // Verificar si la transferencia ya está anulada
                var estadoAnulado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "anulado")?.Id.ToString();
                if (regTrans.Estado == estadoAnulado)
                {
                    // Salir de la operación si la transferencia ya fue anulada
                    return BadRequest("La transferencia ya ha sido anulada y no puede ser procesada.");
                }


                if (operacion == "aceptar")
                {
                    if (regTrans.Estado == listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "por reclamar").SingleOrDefault()!.Id.ToString())
                    {
                        // Es un reclamo
                        regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "reclamado").SingleOrDefault()!.Id.ToString();
                        regTrans.FechaRecibo = DateTime.UtcNow;

                        var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == imageTransferModel.TransferenciaId).ToListAsync();
                        var newEstadoPalet = await _context.Catalogos!
                            .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "reclamado")
                            .SingleAsync();

                        foreach (var det in detalles)
                        {
                            var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                            regPalet.Estado = newEstadoPalet.Id.ToString();
                            regPalet.ApplicationUserId = regTrans.ApplicationUserIdEnvia;
                        }

                    }
                    else
                    {
                        // Procesamiento normal para transferencias
                        regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
                        regTrans.FechaRecibo = DateTime.UtcNow;

                        // Obtener todos los detalles de la transferencia
                        var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == imageTransferModel.TransferenciaId).ToListAsync();
                        var newEstadoPalet = await _context.Catalogos!
                            .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible")
                            .SingleAsync();

                        var estadoReclamado = await _context.Catalogos!
                            .Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "reclamado")
                            .SingleOrDefaultAsync();

                        foreach (var det in detalles)
                        {
                            var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();

                            // Validar si el pallet está en estado "Reclamado"
                            if (estadoReclamado != null && regPalet.Estado == estadoReclamado.Id.ToString())
                            {
                                // Registrar que el pallet no puede ser asignado porque ya fue reclamado
                                /*
                                await Utils.SendNotification(
                                    viewTrans.UserEnviaFirebaseToken,
                                    viewTrans.UserEnviaEmail ?? string.Empty,
                                    viewTrans.UserEnviaFullName ?? string.Empty,
                                    "Pallet reclamado",
                                    $"El pallet con ID {regPalet.Id} ya fue reclamado y no puede ser transferido."
                                );
                                */
                                _notification.Success($"El {regPalet.Descripcion} ya fue reclamado, por lo tanto, no se te fue asignado");
                                continue; // Pasar al siguiente pallet
                            }

                            // Si el pallet no está reclamado, asignarlo al usuario receptor
                            regPalet.Estado = newEstadoPalet.Id.ToString();
                            regPalet.ApplicationUserId = regTrans.ApplicationUserIdRecibe;

                        }

                    }

                }
                else if (operacion == "rechazar")
                {
                    regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
                    regTrans.FechaRechazo = DateTime.UtcNow;

                    var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == imageTransferModel.TransferenciaId).ToListAsync();

                    var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                    foreach (var det in detalles)
                    {
                        var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                        regPalet.Estado = newEstadoPalet.Id.ToString();
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


                return StatusCode(StatusCodes.Status201Created);

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }


        }




        //PROCESA LA TRANSFERENCIA CON IMAGEN CUANDO EL USUARIO RECIBE [ORIGINAL]   NO RECLAMOS   
        /*
        [HttpPost]
        [Route("api/ApiAccess/ProcessTransfer")]
        public async Task<IActionResult> ProcessTransfer([FromBody] ImageTransferModel imageTransferModel)
        {

            try {
                // Codigo para recibir imagene cuando procesen la transferencia
                /*
                string uniqueFileName = "";

                if (imageTransferModel.ImageArray != null)
                {
                    var stream = new MemoryStream(imageTransferModel.ImageArray);

                    var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "thumbnails");
                    uniqueFileName = Guid.NewGuid().ToString() + ".jpg";
                    var filePath = Path.Combine(folderPath, uniqueFileName);
                    using (FileStream fileStream = System.IO.File.Create(filePath))
                    {
                        stream.CopyTo(fileStream);
                    }

                }
                

                var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();

                string operacion = imageTransferModel.Estado;

                var regTrans = await _context.Transferencias!.Where(x => x.Id == imageTransferModel.TransferenciaId).SingleAsync();
                regTrans!.Observaciones = imageTransferModel.Observaciones;

                // Verificar si la transferencia ya está anulada
                var estadoAnulado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "anulado")?.Id.ToString();
                if (regTrans.Estado == estadoAnulado)
                {
                    // Salir de la operación si la transferencia ya fue anulada
                    return BadRequest("La transferencia ya ha sido anulada y no puede ser procesada.");
                }



                if (operacion == "aceptar")
                {
                    regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "recibido").SingleOrDefault()!.Id.ToString();
                    regTrans.FechaRecibo = DateTime.UtcNow;

                    var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == imageTransferModel.TransferenciaId).ToListAsync();

                    var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                    foreach (var det in detalles)
                    {
                        var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                        regPalet.Estado = newEstadoPalet.Id.ToString();
                        regPalet.ApplicationUserId = regTrans.ApplicationUserIdRecibe;
                    }

                }
                else if (operacion == "rechazar")
                {
                    regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "rechazado").SingleOrDefault()!.Id.ToString();
                    regTrans.FechaRechazo = DateTime.UtcNow;

                    var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == imageTransferModel.TransferenciaId).ToListAsync();

                    var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                    foreach (var det in detalles)
                    {
                        var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                        regPalet.Estado = newEstadoPalet.Id.ToString();
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


                return StatusCode(StatusCodes.Status201Created);

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }


        }
        

*/





        [HttpPost]
        [Route("api/ApiAccess/AnularTransfer")]
        public async Task<IActionResult> AnularTransfer([FromBody] ImageTransferModel imageTransferModel)
        {

            try
            {
                var listEstadosTrans = _context.Catalogos!.Where(x => x.Categoria == "estado_transferencia").ToList();
                
                // Obtener el registro de la transferencia
                var regTrans = await _context.Transferencias!.Where(x => x.Id == imageTransferModel.TransferenciaId).SingleAsync();

                // Verificar si el estado es "aceptado" o "rechazado"
                //var estadoAnulado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "anulado")?.Id.ToString();
                var estadoAceptado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "aceptado")?.Id.ToString();
                var estadoRechazado = listEstadosTrans.SingleOrDefault(x => x.Descripcion!.ToLower() == "rechazado")?.Id.ToString();

                if (regTrans.Estado == estadoAceptado || regTrans.Estado == estadoRechazado)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, "La transferencia ya ha sido procesada y no se puede anular.");
                }

                // Actualizar el estado a "anulado" y agregar la observación
                regTrans!.Observaciones = imageTransferModel.Observaciones;
                regTrans.Estado = listEstadosTrans.Where(x => x.Descripcion!.ToLower() == "anulado").SingleOrDefault()!.Id.ToString();
                regTrans.FechaAnulado = DateTime.UtcNow;

                // Actualizar el estado de los pallets asociados a "disponible"
                var detalles = await _context.Detalles!.Where(x => x.IdTransferencia == imageTransferModel.TransferenciaId).ToListAsync();

                var newEstadoPalet = await _context.Catalogos!.Where(x => x.Categoria == "estado_palets" && x.Descripcion!.ToLower() == "disponible").SingleAsync();

                foreach (var det in detalles)
                {
                    var regPalet = await _context.Palets!.Where(x => x.Id == det.IdPalet).SingleAsync();
                    regPalet.Estado = newEstadoPalet.Id.ToString();
                }

                await _context.SaveChangesAsync();


                return StatusCode(StatusCodes.Status201Created);

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }


        }



        [HttpGet]
        [Route("api/ApiAccess/GetCatalogoByCategory")]
        public async Task<ActionResult<List<CatalogoVM>>> GetCatalogoByCategory(string cat)
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
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            return CreatedAtAction("GetCatalogoByCategory", lista);

        }

        [HttpGet]
        [Route("api/ApiAccess/GetRoles")]
        public async Task<ActionResult<RolVM>> GetRoles()
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
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            return CreatedAtAction("GetRoles", lista);

        }

        [HttpPost]
        [Route("api/ApiAccess/AddClient")]
        public async Task<ActionResult<string>> AddClient([FromBody] RegisterUserVM regVM)
        {

            var checkUserByEmail = await _userManager.FindByEmailAsync(regVM.Email);
            if (checkUserByEmail != null)
            {
                return Problem("Email ya existe");
            }
            var checkUserByUsername = await _userManager.FindByNameAsync(regVM.UserName);
            if (checkUserByUsername != null)
            {
                return Problem("Usuario ya existe");
                
            }

            if (regVM.Documento!.Length >= 10)
            {
                var ct = _context.UsersView!.Count(x => regVM.Documento!.StartsWith(x.Documento!));
                if (ct > 0) {
                    return Problem("Usuario ya existe");
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
                    return Problem("Rol no existe");

                await _userManager.AddToRoleAsync(applicationUser, newRegRol.Name);

               
            }
            
            return CreatedAtAction("AddClient", "Ok, el registro se realizo con exito");
        }


        [HttpPost]
        [Route("api/ApiAccess/ChangePasswordClient")]
        public async Task<ActionResult<string>> ChangePasswordClient([FromBody] RegisterUserVM regVM)
        {

            var checkUserByUsername = await _userManager.FindByNameAsync(regVM.UserName);
            if (checkUserByUsername == null)
            {
                return Problem("Usuario no existe");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(checkUserByUsername);
            var result = await _userManager.ResetPasswordAsync(checkUserByUsername, token, regVM.Password);
            if (!result.Succeeded)
            {
                return Problem("Sucedio un error al cambiar la contraseña");
            }

            return CreatedAtAction("ChangePasswordClient", "Ok, la Contraseña se cambio con exito");
            
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}




