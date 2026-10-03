namespace PaletsWebApp.Models
{
    public class Detalle
    {

        public int Id { get; set; }
        public int IdPalet { get; set; }
        public int IdTransferencia { get; set; }
        public int? IdDetalleOrigen { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaEstado { get; set; }
        public string? Observaciones { get; set; }
        public string? ApplicationUserIdResuelve { get; set; }
        public string? EstadoPaletAnterior { get; set; }
        public string? ApplicationUserIdCustodioAnterior { get; set; }
    }
}
