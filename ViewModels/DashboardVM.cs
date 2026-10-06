using PaletsWebApp.Models;

namespace PaletsWebApp.ViewModels;

public class DashboardVM
{
    public string UserName { get; set; } = string.Empty;
    public int TotalPalets { get; set; }
    public int PaletsDisponibles { get; set; }
    public int PaletsEnTransferencia { get; set; }
    public int TransferenciasPendientes { get; set; }
    public Dictionary<string, int> PaletsPorEstado { get; set; } = new();
    public Dictionary<string, int> TransferenciasPorEstado { get; set; } = new();
    public List<View_Transferencia> TransferenciasRecientes { get; set; } = new();
}
