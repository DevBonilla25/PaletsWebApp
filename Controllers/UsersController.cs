using AspNetCoreHero.ToastNotification.Abstractions;
using PaletsWebApp.Models;
using PaletsWebApp.Utilites;
using PaletsWebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace PaletsWebApp.Controllers
{
    
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private IConfiguration _configuration;
        public INotyfService _notification { get; }
        public UsersController(ApplicationDbContext context, 
                               UserManager<ApplicationUser> userManager, 
                               SignInManager<ApplicationUser> signInManager, 
                               INotyfService notyfService,
                               RoleManager<IdentityRole> roleManager,
                               IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _notification = notyfService;
            _roleManager = roleManager;
            _configuration = configuration;

        }
        [Authorize(Roles = "Admin,Supervisor")]
        [HttpGet]
        public async Task<IActionResult> Index(
            string sortOrder,
            string filterUser,
            string sUser,
            string filterEstado,
            string sEstado,
            string filterRol,
            string sRol,
            int? pageNumber,
            int pageSize = 15)
        {

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (sEstado != null) pageNumber = 1;
            else sEstado = filterEstado;

            if (sUser != null) pageNumber = 1;
            else sUser = filterUser;


            if (sRol != null) pageNumber = 1;
            else sRol = filterRol;

            ViewData["filterUser"] = sUser;
            ViewData["filterEstado"] = sEstado;
            ViewData["filterRol"] = sRol;

            var Usuarios = from s in _context.UsersView select s;

            if (!String.IsNullOrEmpty(sUser))
            {
                Usuarios = Usuarios.Where(s => s.Nombres!.Contains(sUser) || s.Apellidos!.Contains(sUser));
            }

            if (!String.IsNullOrEmpty(sRol) && sRol != "-1")
            {
                Usuarios = Usuarios.Where(s => s.RolName!.Equals(sRol));
            }


            if (!String.IsNullOrEmpty(sEstado) && sEstado != "-1")
            {
                if(sEstado == "true")
                    Usuarios = Usuarios.Where(s => s.Activo == true);
                else if (sEstado == "false")
                    Usuarios = Usuarios.Where(s => s.Activo == false);
            }


            switch (sortOrder)
            {
                case "name_desc":
                    Usuarios = Usuarios.OrderByDescending(s => s.Apellidos);
                    break;
                case "Date":
                    Usuarios = Usuarios.OrderBy(s => s.FechaCreacion);
                    break;
                case "date_desc":
                    Usuarios = Usuarios.OrderByDescending(s => s.FechaCreacion);
                    break;
                default:
                    Usuarios = Usuarios.OrderByDescending(s => s.Apellidos);
                    break;
            }

            pageSize = new[] { 15, 25, 50 }.Contains(pageSize) ? pageSize : 15;
            ViewData["PageSize"] = pageSize;
            return View(await PaginatedList<View_User>.CreateAsync(Usuarios.AsNoTracking(), pageNumber ?? 1, pageSize));


        }



        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var existingUser = await _userManager.FindByIdAsync(id);
            if (existingUser == null)
            {
                _notification.Error("Usuario no existe");
                return View();
            }
            var vm = new ResetPasswordVM()
            {
                Id = existingUser.Id,
                UserName = existingUser.UserName,
                UserFullName = existingUser.Nombres + " " + existingUser.Apellidos,
            };
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
        {
            if (!ModelState.IsValid){ return View(vm); }
            var existingUser = await _userManager.FindByIdAsync(vm.Id);
            if (existingUser == null)
            {
                _notification.Error("Usuario no existe");
                return View(vm);
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
            var result = await _userManager.ResetPasswordAsync(existingUser, token, vm.NewPassword);
            if (result.Succeeded)
            {
                _notification.Success("Contraseña se cambio con exito");
                var retEmailmsg = Utils.SendEmailNotification(existingUser.Email, existingUser.Nombres + " " + existingUser.Apellidos, "Tu contraseña ha sido cambiada", "tu contraseña ha sido actualizada a: " + vm.NewPassword);
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }


        [Authorize(Roles ="Admin,Supervisor")]
        [HttpGet]
        public IActionResult Register()
        {

            var vm = new RegisterUserVM();

            var lstRoles = from de in _roleManager.Roles
                           select new SelectListItem
                           {
                               Value = de.Id.ToString(),
                               Text = de.Name,
                               Selected = false
                           };

            var lstTdocs = from dr in _context.Catalogos
                           where dr.Categoria == "tipo_documento"
                           select new SelectListItem
                           {
                               Value = dr.Descripcion,
                               Text = dr.Descripcion,
                               Selected = false
                           };


            vm.RolesList = lstRoles.ToList();
            vm.TiposDocumentosList = lstTdocs.ToList();


            return View(vm);
        }

        [Authorize(Roles = "Admin,Supervisor")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterUserVM vm)
        {

            var lstRoles = from de in _roleManager.Roles
                           select new SelectListItem
                           {
                               Value = de.Id.ToString(),
                               Text = de.Name,
                               Selected = vm.Rol == de.Id ? true : false
                           };

            var lstTdocs = from dr in _context.Catalogos
                           where dr.Categoria == "tipo_documento"
                           select new SelectListItem
                           {
                               Value = dr.Descripcion,
                               Text = dr.Descripcion,
                               Selected = dr.Descripcion == vm.TipoDocumento ? true : false
                           };

            vm.RolesList = lstRoles.ToList();
            vm.TiposDocumentosList = lstTdocs.ToList();


            if (!ModelState.IsValid) { return View(vm); }


            if (string.IsNullOrEmpty(vm.TipoDocumento))
            {
                return View(vm);
            }

            if (string.IsNullOrEmpty(vm.Documento))
            {
                return View(vm);
            }


            var checkUserByEmail = await _userManager.FindByEmailAsync(vm.Email);
            if (checkUserByEmail != null)
            {
                _notification.Error("Email ya existe");
                return View(vm);
            }
            var checkUserByUsername = await _userManager.FindByNameAsync(vm.UserName);
            if (checkUserByUsername != null)
            {
                _notification.Error("Username ya existe");
                return View(vm);
            }

            var applicationUser = new ApplicationUser()
            {
                Email = vm.Email,
                UserName = vm.UserName,
                Nombres = vm.Nombres,
                Apellidos = vm.Apellidos,
                TipoDocumento = vm.TipoDocumento,
                Documento = vm.Documento,
                RazonSocial = vm.RazonSocial,
                Activo = true
            };

            var result =   await _userManager.CreateAsync(applicationUser,vm.Password);
            if (result.Succeeded)
            {

                var newRegRol = (from de in _roleManager.Roles
                                 where de.Id == vm.Rol
                                 select de).FirstOrDefault();


                if (newRegRol?.Name == null) { return BadRequest("El rol seleccionado no existe."); }
                await _userManager.AddToRoleAsync(applicationUser, newRegRol.Name);
                
                _notification.Success("Usuario registrado con exito");
                return RedirectToAction("Index", "Users");
            }
            return View(vm);
        }


      


        [Authorize(Roles = "Admin,Supervisor")]
        [HttpGet("Editar")]
        public async Task<IActionResult> Editar(string id)
        {

            var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (regUser == null) { return NotFound(); }

            var roles = await _userManager.GetRolesAsync(regUser);
            string rolUser = roles.FirstOrDefault()!;

            var regRol = (from de in _roleManager.Roles
                         where de.Name == rolUser
                         select de).FirstOrDefault();


            RegisterUserVM vm = new RegisterUserVM();

            vm.Id = id;
            vm.Nombres = regUser.Nombres;
            vm.Apellidos = regUser.Apellidos;
            vm.TipoDocumento = regUser.TipoDocumento;
            vm.Documento = regUser.Documento;
            vm.Email = regUser.Email;
            vm.Telefono = regUser.Telefono;
            vm.Direccion = regUser.Direccion;
            vm.RazonSocial = regUser.RazonSocial;
            vm.Rol = regRol!.Id;
            vm.Activo = (bool)regUser.Activo!;


            var lstRoles = from de in _roleManager.Roles
                             select new SelectListItem
                             {
                                 Value = de.Id.ToString(),
                                 Text = de.Name,
                                 Selected = false
                             };

            var lstTdocs = from dr in _context.Catalogos
                           where dr.Categoria == "tipo_documento"
                           select new SelectListItem
                           {
                               Value = dr.Descripcion,
                               Text = dr.Descripcion,
                               Selected = false
                           };


            vm.RolesList = lstRoles.ToList();
            vm.TiposDocumentosList = lstTdocs.ToList();
            

            return View(vm);

        }

        [Authorize(Roles = "Admin,Supervisor")]
        [HttpPost("Editar")]
        public async Task<IActionResult> Editar(RegisterUserVM vm)
        {


            var ct = _context.Palets!.Where(x => x.ApplicationUserId == vm.Id).Count();

            bool invalidInactive = ct > 0 && !vm.Activo ? true : false;


            if (!string.IsNullOrEmpty(vm.Nombres) &&
               !string.IsNullOrEmpty(vm.Apellidos) &&
               !string.IsNullOrEmpty(vm.Email) &&
               !string.IsNullOrEmpty(vm.TipoDocumento) &&
               !string.IsNullOrEmpty(vm.Documento) &&
               !string.IsNullOrEmpty(vm.Rol) && !invalidInactive)
            {

                var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == vm.Id);
                if (regUser == null) { return NotFound(); }

                regUser.Nombres = vm.Nombres;
                regUser.Apellidos = vm.Apellidos;
                regUser.Email = vm.Email;
                regUser.TipoDocumento = vm.TipoDocumento;
                regUser.Documento = vm.Documento;
                regUser.Direccion = vm.Direccion;
                regUser.RazonSocial = vm.RazonSocial;
                regUser.Telefono = vm.Telefono;
                regUser.Activo = vm.Activo;


                await _userManager.UpdateAsync(regUser);

                var roles = await _userManager.GetRolesAsync(regUser);
                string rolUser = roles.FirstOrDefault()!;

                var regRol = (from de in _roleManager.Roles
                              where de.Name == rolUser  
                              select de).FirstOrDefault();

                var newRegRol = (from de in _roleManager.Roles
                                 where de.Id == vm.Rol
                                 select de).FirstOrDefault();


                if (regRol?.Name == null || newRegRol?.Name == null)
                {
                    return BadRequest("No se pudo determinar el rol del usuario.");
                }

                await _userManager.RemoveFromRoleAsync(regUser, regRol.Name);
                await _userManager.AddToRoleAsync(regUser, newRegRol.Name);


                string fullNameUser = regUser.Nombres + " " + regUser.Apellidos;

                if (vm.Activo)
                {
                    var retEmailmsg = Utils.SendEmailNotification(regUser.Email, fullNameUser, "Tu cuenta ha sido activada", "Tu cuenta ha sido activada, puedes ingresar a la aplicación con las credenciales con las que te has registrado, tu Usuario para ingresar es : " + regUser.UserName);
                }
                else {
                    var retEmailmsg = Utils.SendEmailNotification(regUser.Email, fullNameUser,  "Tu cuenta ha sido desactivada", "Tu cuenta ha sido desactivada, para cualquier consulta contactate con el administrador del sistema");
                }
                

                _notification.Success("Actualización de usuario Exitoso");

                return RedirectToAction("Index", "Users");

            }



            var lstRoles = from de in _roleManager.Roles
                           select new SelectListItem
                           {
                               Value = de.Id.ToString(),
                               Text = de.Name,
                               Selected = vm.Rol == de.Id ? true : false
                           };

            var lstTdocs = from dr in _context.Catalogos
                           where dr.Categoria == "tipo_documento"
                           select new SelectListItem
                           {
                               Value = dr.Descripcion,
                               Text = dr.Descripcion,
                               Selected = dr.Descripcion == vm.TipoDocumento ? true : false
                           };


            if (invalidInactive)
            {
                _notification.Information("El usuario no se puede desactivar con pallets asignados");
            }


            vm.RolesList = lstRoles.ToList();
            vm.TiposDocumentosList = lstTdocs.ToList();

            return View(vm);

        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (!HttpContext.User.Identity!.IsAuthenticated)
            {
                return View(new LoginVM());
            }
            return RedirectToAction("Index", "Palets");
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginVM vm)
        {

            ViewBag.Activo = true;

            if (!ModelState.IsValid) { return View(vm); }
            var existingUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == vm.Username);
            if(existingUser == null)
            {
                _notification.Error("Usuario no existe");
                return View(vm);
            }

            if (existingUser.Activo == false)
            {
                _notification.Error("Cuenta desactivada");
                ViewBag.Activo = false;
                return View(vm);
            }


            var verifyPassword = await _userManager.CheckPasswordAsync(existingUser, vm.Password);
            if (!verifyPassword)
            {
                _notification.Error("Contraseña invalida");
                return View(vm);
            }
            await _signInManager.PasswordSignInAsync(vm.Username, vm.Password, vm.RememberMe, true);
            //_notification.Success("Login Exitoso");
            return RedirectToAction("Index", "Palets");
        }


        [Authorize]
        [HttpGet("Perfil")]
        public async Task<IActionResult> Perfil()
        {
            var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            if (regUser == null) { return Unauthorized(); }
            
            RegisterUserVM vm = new RegisterUserVM();

            vm.Id = regUser.Id;
            vm.Nombres = regUser.Nombres;
            vm.Apellidos = regUser.Apellidos;
            vm.TipoDocumento = regUser.TipoDocumento;
            vm.Email = regUser.Email;
            vm.UserName = regUser.UserName;
            vm.Documento = regUser.Documento;
            vm.Telefono = regUser.Telefono;
            vm.Direccion = regUser.Direccion;
            vm.Rol = "--";
            vm.Password = "--";

            var lstTdocs = from dr in _context.Catalogos
                           where dr.Categoria == "tipo_documento"
                           select new SelectListItem
                           {
                               Value = dr.Descripcion,
                               Text = dr.Descripcion,
                               Selected = false
                           };


            vm.TiposDocumentosList = lstTdocs.ToList();

            return View(vm);

        }

        [Authorize]
        [HttpPost("Perfil")]
        public async Task<IActionResult> Perfil(RegisterUserVM vm)
        {
            var lstTdocs = from dr in _context.Catalogos
                           where dr.Categoria == "tipo_documento"
                           select new SelectListItem
                           {
                               Value = dr.Descripcion,
                               Text = dr.Descripcion,
                               Selected = dr.Descripcion == vm.TipoDocumento ? true : false
                           };

            vm.TiposDocumentosList = lstTdocs.ToList();

            if (!ModelState.IsValid) { 
                return View(vm); 
            }

            var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            if (regUser == null) { return Unauthorized(); }
            
            regUser.Nombres = vm.Nombres;
            regUser.Apellidos = vm.Apellidos;
            regUser.TipoDocumento = vm.TipoDocumento;
            regUser.Documento = vm.Documento;
            regUser.Telefono = vm.Telefono;
            regUser.Direccion = vm.Direccion;

            await _userManager.UpdateAsync(regUser);

            _notification.Success("Actualización de perfil Exitoso");

            return RedirectToAction("Index", "Users");

        }

        [HttpGet]
        public IActionResult ForgetPassword([FromQuery(Name = "m")] string m)
        {
            ViewBag.LinkEnviado = false;
            ViewBag.FomMobile = false;

            if (!string.IsNullOrEmpty(m))
            {
                ViewBag.FomMobile = true;
                return View(new ForgetPasswordVM { 
                    FromMobile = true
                });
            }

            return View(new ForgetPasswordVM { FromMobile = false});
            
        }


        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM vm)
        {

            ViewBag.LinkEnviado = false;
            ViewBag.FomMobile = vm.FromMobile;

            if (string.IsNullOrEmpty(vm.Email))
            {
                _notification.Error("El email es requerido");
                return View(vm);
            }

            var regUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == vm.Email);

            if (regUser == null)
            {
                _notification.Error("El email no existe");
                return View(vm);
            }

            string fullNameUser = regUser.Nombres + " " + regUser.Apellidos;

            var token = await _userManager.GeneratePasswordResetTokenAsync(regUser);
            var encodedToken = Encoding.UTF8.GetBytes(token);
            var validToken = WebEncoders.Base64UrlEncode(encodedToken);

            string url = $"{Request.Scheme}://{Request.Host}/users/ResetPasswordForget?email={vm.Email}&token={validToken}";
            //string url = $"{_configuration["AppUrl"]}/ResetPasswordForget?email={vm.Email}&token={validToken}";

            var retEmailmsg = Utils.SendEmailNotification(regUser.Email, fullNameUser, "Recuperar contraseña", "<h1>Siga las intrucciones para recuperar su contraseña</h1>" +
                $"<p>para recuperar su contraseña haga <a href='{url}'>Click aqui</a></p>");


            ViewBag.LinkEnviado = true;

            _notification.Success("¡La URL de restablecimiento de su contraseña se ha enviado a su correo electrónico!");

            return View(new ForgetPasswordVM());

        }
        

        [HttpGet]
        public IActionResult ResetPasswordForget([FromQuery(Name = "email")] string email,
                                                             [FromQuery(Name = "token")] string token)
        {

            ViewBag.CambioExitoso = false;

            var vm = new ResetPasswordForgetViewModel();
            vm.Token = token;
            vm.Email = email;

            return View(vm);
        }


        [HttpPost("ResetPasswordForget")]
        public async Task<IActionResult> ResetPasswordForget([FromForm] ResetPasswordForgetViewModel vm)
        {

            ViewBag.CambioExitoso = false;

            if (ModelState.IsValid)
            {

                var user = await _userManager.FindByEmailAsync(vm.Email);

                if (user == null)
                {
                    _notification.Error("No existe el email");
                    return View(vm);
                }

                if (vm.NewPassword != vm.ConfirmPassword) {
                    _notification.Error("Contraseña y confirmar contraseña no coinciden");
                    return View(vm);
                }

                var decodedToken = WebEncoders.Base64UrlDecode(vm.Token);
                string normalToken = Encoding.UTF8.GetString(decodedToken);

                var result = await _userManager.ResetPasswordAsync(user, normalToken, vm.NewPassword);

                if (!result.Succeeded)
                {
                    _notification.Error("Ha sucedido algun error al cambiar la contraseña");
                    return View(vm);
                }

                _notification.Success("¡La contraseña se ha cambiado exitosamente!");

                ViewBag.CambioExitoso = true;

                return View(new ResetPasswordForgetViewModel());

            }

            return BadRequest("Some properties are not valid");
        }




        [HttpPost]
        [Authorize]
        public IActionResult Logout()
        {
            _signInManager.SignOutAsync();
            //_notification.Success("Cerro sesión exitosamente");
            return RedirectToAction("Index", "Home");
        }


        [HttpGet("AccessDenied")]
        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }


    }
}


