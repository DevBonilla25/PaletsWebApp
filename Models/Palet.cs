namespace PaletsWebApp.Models
{
    public class Palet
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public string? Observaciones { get; set; }
        public string? Estado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

    }

    public class View_Palet
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public string? Observaciones { get; set; }
        public string? Estado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string? ApplicationUserId { get; set; }

        public string? UserFullName { get; set; }

        public string? DescEstado { get; set; }

    }


}
