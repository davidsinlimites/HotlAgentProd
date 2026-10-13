using DotNetEnv;
using HotelAgentIA.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Utilidad para las estancias de ejemplo: las deja activas desde ayer hasta dentro de 30 dias
/// en la base apuntada por POSTGRES_CONNECTION. No imprime la cadena de conexion.
/// </summary>
internal static class RenovarEstancias
{
    public static async Task EjecutarAsync()
    {
        Env.NoClobber().TraversePath().Load();
        var conexion = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(conexion))
        {
            Console.WriteLine("Falta POSTGRES_CONNECTION.");
            return;
        }

        var opciones = new DbContextOptionsBuilder<HotelDbContext>().UseNpgsql(conexion).Options;
        await using var db = new HotelDbContext(opciones);

        var ahoraUtc = DateTimeOffset.UtcNow;
        var estancias = await db.Estancias.ToListAsync();
        foreach (var estancia in estancias)
        {
            estancia.Entrada = ahoraUtc.AddDays(-1);
            estancia.Salida = ahoraUtc.AddDays(30);
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"Estancias renovadas: {estancias.Count} (activas hasta {ahoraUtc.AddDays(30):yyyy-MM-dd}).");
    }
}
