using System.Collections.Specialized;

namespace PaletsWebApp.Models
{
    public class Transferencia
    {
        public int Id { get; set; }
        public string? CodigoInterno { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public DateTime FechaRecibo { get; set; }
        public DateTime FechaRechazo { get; set; }
        public DateTime FechaAnulado { get; set; }

        public string? ApplicationUserIdEnvia { get; set; }
     
        public string? ApplicationUserIdRecibe { get; set; }
        
        public string? Estado { get; set; }
        public string? Foto { get; set; }
        public string? Observaciones { get; set; }

    }

    public class View_Transferencia
    {
        public int Id { get; set; }
        public string? CodigoInterno { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public DateTime FechaRecibo { get; set; }
        public DateTime FechaRechazo { get; set; }
        public DateTime FechaAnulado { get; set; }

        public string? ApplicationUserIdEnvia { get; set; }

        public string? ApplicationUserIdRecibe { get; set; }

        public string? Estado { get; set; }
        public string? Foto { get; set; }
        public string? Observaciones { get; set; }

        public string? UserEnviaFullName { get; set; }
        public string? UserRecibeFullName { get; set; }

        public string? UserEnviaFirebaseToken { get; set; }
        public string? UserRecibeFirebaseToken { get; set; }

        public string? UserEnviaEmail { get; set; }
        public string? UserRecibeEmail { get; set; }

        public string? DescEstado { get; set; }
    }
}
