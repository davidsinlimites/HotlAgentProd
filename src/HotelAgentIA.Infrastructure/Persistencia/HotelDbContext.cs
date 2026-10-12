using HotelAgentIA.Infrastructure.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace HotelAgentIA.Infrastructure.Persistencia;

public class HotelDbContext(DbContextOptions<HotelDbContext> opciones) : DbContext(opciones)
{
    public DbSet<RestauranteEntidad> Restaurantes => Set<RestauranteEntidad>();
    public DbSet<HorarioAperturaEntidad> Horarios => Set<HorarioAperturaEntidad>();
    public DbSet<PlatoMenuEntidad> Platos => Set<PlatoMenuEntidad>();
    public DbSet<ActividadEntidad> Actividades => Set<ActividadEntidad>();
    public DbSet<HuespedEntidad> Huespedes => Set<HuespedEntidad>();
    public DbSet<EstanciaEntidad> Estancias => Set<EstanciaEntidad>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<RestauranteEntidad>(e =>
        {
            e.ToTable("restaurantes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Ubicacion).HasMaxLength(200);
            e.HasMany(x => x.Horarios).WithOne().HasForeignKey(x => x.RestauranteId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Platos).WithOne().HasForeignKey(x => x.RestauranteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<HorarioAperturaEntidad>(e =>
        {
            e.ToTable("horarios_apertura");
            e.HasKey(x => x.Id);
            e.Property(x => x.RestauranteId).HasMaxLength(50);
        });

        modelo.Entity<PlatoMenuEntidad>(e =>
        {
            e.ToTable("platos_menu");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.RestauranteId).HasMaxLength(50);
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Precio).HasPrecision(10, 2);
            e.Property(x => x.EtiquetasDieteticas).HasColumnType("text[]");
        });

        modelo.Entity<ActividadEntidad>(e =>
        {
            e.ToTable("actividades");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Ubicacion).HasMaxLength(200);
            e.HasIndex(x => x.Dia);
        });

        modelo.Entity<HuespedEntidad>(e =>
        {
            e.ToTable("huespedes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Nombre).HasMaxLength(200);
        });

        modelo.Entity<EstanciaEntidad>(e =>
        {
            e.ToTable("estancias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.HuespedId).HasMaxLength(50);
            e.Property(x => x.NumeroHabitacion).HasMaxLength(20);
            e.HasOne<HuespedEntidad>().WithMany().HasForeignKey(x => x.HuespedId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.HuespedId);
        });
    }
}
