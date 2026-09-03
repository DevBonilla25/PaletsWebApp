using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaletsWebApp.Models;

namespace PaletsWebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser>? ApplicationUsers { get; set; }

        public DbSet<Catalogo>? Catalogos { get; set; }
        public DbSet<Transferencia>? Transferencias { get; set; }
        public DbSet<Palet>? Palets { get; set; }
        public DbSet<Detalle>? Detalles { get; set; }

        public DbSet<View_Transferencia>? TransferenciasView { get; set; }
        public DbSet<View_Palet>? PaletsView { get; set; }

        public DbSet<View_User>? UsersView { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder
               .Entity<View_Transferencia>()
               .ToView("View_Transferencias")
               .HasKey(t => t.Id);

            modelBuilder
              .Entity<View_Palet>()
              .ToView("View_Palets")
              .HasKey(t => t.Id);

            modelBuilder
              .Entity<View_User>()
              .ToView("View_Users")
              .HasKey(t => t.Id);

        }

    }
}


