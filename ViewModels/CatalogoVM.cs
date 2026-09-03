using Microsoft.Build.Framework;

namespace PaletsWebApp.ViewModels
{
    public class CatalogoVM
    {
        
        public int Id { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Descripcion{ get; set; } = string.Empty;
    }
}



