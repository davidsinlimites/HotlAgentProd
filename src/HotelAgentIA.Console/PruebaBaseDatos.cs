using DotNetEnv;
using HotelAgentIA.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Banco de pruebas de los datos del hotel contra el PostgreSQL real (Docker en local):
/// aplica migraciones, siembra datos de ejemplo y los lee por el repositorio.
/// </summary>
internal static class PruebaBaseDatos
{
    public static async Task EjecutarAsync()
    {
        Console.WriteLine("=== Datos del hotel en PostgreSQL ===");

        Env.NoClobber().TraversePath().Load();
        var conexion = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(conexion))
        {
            Console.WriteLine("Falta POSTGRES_CONNECTION: se omite la prueba.");
            Console.WriteLine();
            return;
        }

        var opciones = new DbContextOptionsBuilder<HotelDbContext>().UseNpgsql(conexion).Options;
        await using var db = new HotelDbContext(opciones);

        await db.Database.MigrateAsync();
        await SemillaDatos.AplicarAsync(db, DateTimeOffset.UtcNow);

        var repositorio = new RepositorioInfoHotel(db);
        var ahora = DateTimeOffset.Now;

        foreach (var restaurante in await repositorio.ObtenerRestaurantesAsync())
        {
            var hoy = restaurante.Horarios.First(h => h.Dia == ahora.DayOfWeek);
            Console.WriteLine($"{restaurante.Nombre} ({restaurante.Ubicacion}): hoy {hoy.Abre:HH\\:mm}-{hoy.Cierra:HH\\:mm}");

            foreach (var plato in await repositorio.ObtenerMenuAsync(restaurante.Id))
            {
                Console.WriteLine($"    {plato.Nombre} - {plato.Precio:0.00} [{string.Join(", ", plato.EtiquetasDieteticas)}] ({string.Join(", ", plato.Comidas)})");
            }
        }

        Console.WriteLine($"Actividades de hoy ({ahora.DayOfWeek}):");
        var actividades = await repositorio.ObtenerActividadesAsync(DateOnly.FromDateTime(ahora.DateTime));
        if (actividades.Count == 0)
        {
            Console.WriteLine("    (ninguna)");
        }

        foreach (var actividad in actividades)
        {
            Console.WriteLine($"    {actividad.Inicio:HH\\:mm}-{actividad.Fin:HH\\:mm} {actividad.Nombre} ({actividad.Ubicacion})");
        }

        foreach (var huesped in new[] { "305", "412", "999" })
        {
            var estancia = await repositorio.ObtenerEstanciaActivaAsync(huesped, ahora);
            Console.WriteLine(estancia is null
                ? $"Huésped {huesped}: sin estancia activa"
                : $"Huésped {huesped}: habitación {estancia.NumeroHabitacion}, activa hasta {estancia.Salida.ToLocalTime():yyyy-MM-dd HH:mm}");
        }

        Console.WriteLine();
    }
}
