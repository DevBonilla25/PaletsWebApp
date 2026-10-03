using PaletsWebApp.Data;
using PaletsWebApp.Models;
using Microsoft.AspNetCore.Identity;

namespace PaletsWebApp.Utilites
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public DbInitializer(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void Initialize()
        {


            var useraAdmin =  _context.ApplicationUsers!.FirstOrDefault(x => x.Email == "hernanjls@gmail.com");

            if (useraAdmin == null)
            {
                _userManager.CreateAsync(new ApplicationUser()
                {
                    UserName = "hernan",
                    Email = "hernanjls@gmail.com",
                    Nombres = "Super",
                    Apellidos = "Admin",
                    TipoDocumento = "Cedula",
                    Documento = "0000000",
                    Direccion = "",
                    Telefono = "",
                    Foto = "",

                }, "123456").Wait();

                var appUser = _context.ApplicationUsers!.FirstOrDefault(x => x.Email == "hernanjls@gmail.com");
                if (appUser != null)
                {
                    _userManager.AddToRoleAsync(appUser, WebsiteRoles.Admin).GetAwaiter().GetResult();
                }
            }


            if (!_roleManager.RoleExistsAsync(WebsiteRoles.Invitado).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(WebsiteRoles.Invitado)).GetAwaiter().GetResult();
                _userManager.CreateAsync(new ApplicationUser()
                {
                    UserName = "invitado",
                    Email = "invitado@gmail.com",
                    Nombres = "Usuario",
                    Apellidos = "Invitado",
                    TipoDocumento = "Cedula",
                    Documento = "999999999",
                    Direccion = "",
                    Telefono = "",
                    Foto = "",

                }, "123456").Wait();

                var appUser = _context.ApplicationUsers!.FirstOrDefault(x => x.Email == "invitado@gmail.com");
                if (appUser != null)
                {
                    _userManager.AddToRoleAsync(appUser, WebsiteRoles.Invitado).GetAwaiter().GetResult();
                }

            }


            if (!_roleManager.RoleExistsAsync(WebsiteRoles.Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(WebsiteRoles.Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(WebsiteRoles.Cliente)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(WebsiteRoles.Bodeguero)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(WebsiteRoles.Chofer)).GetAwaiter().GetResult();
                _userManager.CreateAsync(new ApplicationUser()
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Nombres ="Super",
                    Apellidos = "Admin",
                    TipoDocumento = "Cedula",
                    Documento = "0000000",
                    Direccion = "",
                    Telefono = "",
                    Foto = "",
                   
                },"Admin@0011").Wait();

                var appUser = _context.ApplicationUsers!.FirstOrDefault(x => x.Email == "admin@gmail.com");
                if (appUser != null)
                {
                    _userManager.AddToRoleAsync(appUser, WebsiteRoles.Admin).GetAwaiter().GetResult();
                }


                var listOfCatalogs = new List<Catalogo>()
                {
                    new Catalogo()
                    {
                        Categoria = "estado_palets",
                        Descripcion = "Disponible",
                    },
                    new Catalogo()
                    {
                        Categoria = "estado_palets",
                        Descripcion = "En transferencia",
                    },
                    new Catalogo()
                    {
                        Categoria = "estado_palets",
                        Descripcion = "Dado de baja",
                    },
                    new Catalogo()
                    {
                        Categoria = "estado_transferencia",
                        Descripcion = "Por recibir",
                    },
                    new Catalogo()
                    {
                        Categoria = "estado_transferencia",
                        Descripcion = "Recibido",
                    },
                    new Catalogo()
                    {
                        Categoria = "estado_transferencia",
                        Descripcion = "Rechazado",
                    },
                     new Catalogo()
                    {
                        Categoria = "estado_transferencia",
                        Descripcion = "Anulado",
                    },
                    new Catalogo()
                    {
                        Categoria = "tipo_documento",
                        Descripcion = "Cedula",
                    },
                    new Catalogo()
                    {
                        Categoria = "tipo_documento",
                        Descripcion = "Ruc",
                    },
                    new Catalogo()
                    {
                        Categoria = "tipo_documento",
                        Descripcion = "Pasaporte",
                    },
                 };

                _context.Catalogos!.AddRange(listOfCatalogs);
                _context.SaveChanges();

            }

            if (!_roleManager.RoleExistsAsync(WebsiteRoles.Supervisor).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(WebsiteRoles.Supervisor)).GetAwaiter().GetResult();
            }
        }
    }
}

