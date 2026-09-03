using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using PaletsWebApp.Models;
using X.PagedList;

namespace PaletsWebApp.ViewModels
{

    
    public class TransferenciaVM
    {
        public int Id { get; set; }
        public string? CodigoInterno { get; set; }
        public DateTime FechaEnvio { get; set; } 
        public DateTime FechaRecibo { get; set; }
        public DateTime FechaRechazo { get; set; }
        public DateTime FechaAnulado { get; set; }

        public string IdUserEnvia { get; set; } = string.Empty;
        public string? NombreUserEnvia { get; set; }

        [Required(ErrorMessage ="El usuario que recibe es requerido")]
        public string IdUserRecibe { get; set; } = string.Empty;

        public string? NombreUserRecibe { get; set; }
        public string? Estado { get; set; }
        public string? DescEstado { get; set; }
        public string? Foto { get; set; }
        public IFormFile? FotoFile { get; set; }
        public string? Observaciones { get; set; }

        [Required]
        public List<PaletVM> Palets { get; set; } = new();

        [Required(ErrorMessage ="Por lo menos un pallet es requerido")]
        public List<string> UqChecked { get; set; } = new();
        public string? JsonPalets { get; set; }

        public List<SelectListItem>? UserList { get; set; }


    }

    

}

