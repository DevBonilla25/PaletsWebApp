namespace PaletsWebApp.Models
{
    public class Catalogo
    {

        public int Id { get; set; }
        public string? Categoria { get; set; }
        public int Minimo { get; set; }
        public int Maximo { get; set; }
        public string? Descripcion { get; set; }

    }
}
