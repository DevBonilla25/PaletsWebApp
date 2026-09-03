using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace PaletsWebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? TipoDocumento { get; set; }
        public string? Documento { get; set; }
        public string? Direccion { get; set; }
        public string? RazonSocial { get; set; }
        public string? Telefono { get; set; }
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;
        public string? Foto { get; set; }
        public bool? Activo { get; set; } = false;
        public bool? Eliminado { get; set; } = false;
        public string? FirebaseToken { get; set; } 

        //relation
        public List<Palet>? Palets { get; set; }
    }

    public class View_User 
    {
        public string? Id { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? TipoDocumento { get; set; }
        public string? Documento { get; set; }
        public string? Direccion { get; set; }
        public string? RazonSocial { get; set; }
        public string? Telefono { get; set; }
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;
        public string? Foto { get; set; }
        public bool? Activo { get; set; } = false;
        public bool? Eliminado { get; set; } = false;
        public string? FirebaseToken { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? RolName { get; set; }

    }

}
