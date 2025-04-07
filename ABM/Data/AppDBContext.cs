using Microsoft.EntityFrameworkCore;
using ABM.Models;
namespace ABM.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {

        }

        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Rol> Rol { get; set; }
        public DbSet<Gerencia> Gerencia { get; set; }
        public DbSet<Subgerencia> Subgerencia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(tb =>
            {
                tb.HasKey(col => col.IdUsuario);
                tb.Property(col => col.IdUsuario).UseIdentityColumn().ValueGeneratedOnAdd();

                tb.Property(col => col.nombre).HasMaxLength(50);
                tb.Property(col => col.correo).HasMaxLength(50);
                tb.Property(col => col.password_c).HasMaxLength(50);
                tb.Property(col => col.password).HasColumnType("varchar(max)");
                tb.Property(col => col.TokenRecuperacion).HasMaxLength(200);

                tb.Property(col => col.ExpiracionToken).IsRequired(false);
                tb.Property(col => col.estado).HasColumnType("char(1)").IsRequired(false);
                tb.Property(col => col.ResponsableFirma).HasColumnType("bit").IsRequired(false);
                tb.Property(u => u.COD_OTC);
                tb.Property(u => u.MesesExpiracionClave);
                tb.HasOne(u => u.Gerencia)
              .WithMany() 
              .HasForeignKey(u => u.ID_gerencia);

                tb.HasOne(u => u.Rol)
              .WithMany()  // Configura la relación si hay una entidad "Rol" asociada
              .HasForeignKey(u => u.idRol);

                tb.HasOne(u => u.Subgerencia)
                  .WithMany()
                  .HasForeignKey(u => u.ID_Subgerencia)
                  .IsRequired(false);
            });

            modelBuilder.Entity<Rol>(tb =>
            {
                tb.HasKey(r => r.idRol);
                tb.Property(r => r.nombre).HasMaxLength(50);
            });



            modelBuilder.Entity<Gerencia>(tb =>
            {
                tb.HasKey(g => g.ID_gerencia);
                tb.Property(g => g.Nom_Gerencia).HasMaxLength(50);
            });

            modelBuilder.Entity<Subgerencia>(tb =>
            {
                tb.HasKey(s => s.ID_Subgerencia);
                tb.Property(s => s.Nom_Subgerencia).HasMaxLength(50);

                // Relación con Gerencia
                tb.HasOne(s => s.Gerencia)
                  .WithMany()
                  .HasForeignKey(s => s.COD_Gerencia);
            });
            modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Rol>().ToTable("Rol");
            modelBuilder.Entity<Gerencia>().ToTable("im_gerencia");
            modelBuilder.Entity<Subgerencia>().ToTable("im_Subgerencias");

        }

    }
}
