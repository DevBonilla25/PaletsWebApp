using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PaletsWebApp.ViewModels
{
    public class PaletVM 
    {
        public int Id { get; set; }
       
        [Required(ErrorMessage ="Una descripción es requerida")]
        public string Descripcion { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string? Estado { get; set; }
        public string? DescEstado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required(ErrorMessage ="Un usuario es requerido")]
        public string? ApplicationUserId { get; set; }
        public string? ApplicationUserName { get; set; }
        public string? RolUser { get; set; }

        public bool IsSelected { get; set; }
        public string? EstadoDetalle { get; set; }
        public string? DescEstadoDetalle { get; set; }
        public DateTime? FechaEstadoDetalle { get; set; }
        public string? ObservacionesDetalle { get; set; }
        public int? IdDetalleOrigen { get; set; }
        public string? ApplicationUserIdResuelveDetalle { get; set; }
        public bool EsCorreccionCustodia { get; set; }
        public int? TransferenciaPendienteId { get; set; }
        public string? ReceptorTransferenciaPendiente { get; set; }

        public List<SelectListItem>? UserList { get; set; }
        public List<SelectListItem>? EstatusList { get; set; }

        

    }
}

