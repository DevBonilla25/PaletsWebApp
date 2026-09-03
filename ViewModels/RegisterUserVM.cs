using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PaletsWebApp.ViewModels
{
    public class RegisterUserVM
    {
        public string? Id { get; set; }
        [Required]
        public string? Nombres { get; set; }
        [Required]
        public string? Apellidos { get; set; }

        public string? Direccion { get; set; }
        public string? RazonSocial { get; set; }

        public string? Telefono { get; set; }

        [Required]
        public string? TipoDocumento { get; set; }

        [Required]
        public string? Documento { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string? UserName { get; set; }
        [Required]
        public string? Password { get; set; }

        public bool Activo { get; set; }
        public string? FirebaseToken { get; set; }

        [Required]
        public string? Rol { get; set; }

        public List<SelectListItem>? TiposDocumentosList { get; set; }
        public List<SelectListItem>? RolesList { get; set; }

        public string FullName {
            get { 
                return Apellidos + " " + Nombres;
            }
        }


    }
}
