using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelAgentIA.Infrastructure.Persistencia;

/// <summary>
/// Solo la usan las herramientas de EF (dotnet ef) para crear migraciones;
/// la aplicacion configura el contexto por su cuenta.
/// </summary>
public sealed class FabricaHotelDbContext : IDesignTimeDbContextFactory<HotelDbContext>
{
    public HotelDbContext CreateDbContext(string[] args)
    {
        Env.NoClobber().TraversePath().Load();
        var conexion = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("Falta la variable POSTGRES_CONNECTION (en el .env o en el entorno).");

        var opciones = new DbContextOptionsBuilder<HotelDbContext>().UseNpgsql(conexion).Options;
        return new HotelDbContext(opciones);
    }
}
