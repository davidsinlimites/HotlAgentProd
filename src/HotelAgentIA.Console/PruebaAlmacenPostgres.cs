using DotNetEnv;
using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Domain.Memoria;
using HotelAgentIA.Infrastructure.Memoria;
using HotelAgentIA.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Banco de pruebas del almacen clave-valor sobre PostgreSQL real: operaciones basicas, borrado por
/// prefijo (sin confundir huespedes ni comodines LIKE) y escrituras simultaneas del mismo huesped.
/// </summary>
internal static class PruebaAlmacenPostgres
{
    public static async Task EjecutarAsync()
    {
        Console.WriteLine("=== Almacen de memoria en PostgreSQL ===");

        Env.NoClobber().TraversePath().Load();
        var conexion = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(conexion))
        {
            Console.WriteLine("Falta POSTGRES_CONNECTION: se omite la prueba.");
            Console.WriteLine();
            return;
        }

        var opciones = new DbContextOptionsBuilder<HotelDbContext>().UseNpgsql(conexion).Options;
        await using (var db = new HotelDbContext(opciones))
        {
            await db.Database.MigrateAsync();
        }

        var huespedes = new[] { "prueba3", "prueba30", "prueba3x", "prueba_", "pruebaa", "pruebaz" };
        await LimpiarAsync(opciones, huespedes);

        Console.WriteLine("-- Guardar, leer, sobrescribir y eliminar");
        await using (var db = new HotelDbContext(opciones))
        {
            var almacen = new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System);
            var clave = ClavesMemoria.Preferencia("prueba3", "dieta");
            await almacen.GuardarAsync(clave, "vegano");
            await almacen.GuardarAsync(clave, "vegetariano");
            Console.WriteLine($"   leido: {await almacen.ObtenerAsync(clave)}");
            await almacen.EliminarAsync(clave);
            Console.WriteLine($"   tras eliminar: {await almacen.ObtenerAsync(clave) ?? "(nada)"}");
        }

        Console.WriteLine("-- Borrado por prefijo: solo prueba3, no prueba30 ni prueba3x ni los comodines _ y %");
        await using (var db = new HotelDbContext(opciones))
        {
            var almacen = new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System);
            foreach (var id in huespedes)
            {
                await almacen.GuardarAsync(ClavesMemoria.Preferencia(id, "dieta"), "x");
                await almacen.GuardarAsync(ClavesMemoria.Conversacion(id), "[]");
            }

            await almacen.EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped("prueba3"));
            await almacen.EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped("prueba_"));
            foreach (var id in huespedes)
            {
                var n = (await almacen.ObtenerAsync(ClavesMemoria.Preferencia(id, "dieta")) is null ? 0 : 1)
                        + (await almacen.ObtenerAsync(ClavesMemoria.Conversacion(id)) is null ? 0 : 1);
                Console.WriteLine($"   {id}: {n} claves");
            }
        }

        Console.WriteLine("-- 15 turnos simultaneos del mismo huesped (un DbContext por tarea): no debe perderse ninguno");
        await Task.WhenAll(Enumerable.Range(1, 15).Select(async i =>
        {
            await using var db = new HotelDbContext(opciones);
            var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System));
            await servicio.AgregarTurnoAsync(new TurnoConversacion("pruebaz", RolConversacion.Usuario, $"mensaje {i}", DateTimeOffset.UtcNow));
        }));
        await using (var db = new HotelDbContext(opciones))
        {
            var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System));
            var turnos = await servicio.ObtenerUltimosAsync("pruebaz", 20);
            var distintos = turnos.Select(t => t.Contenido).Distinct().Count();
            Console.WriteLine($"   guardados {turnos.Count} turnos, {distintos} distintos (esperado 15 y 15)");
        }

        Console.WriteLine("-- Los mas de 20 se recortan: 25 turnos simultaneos");
        await Task.WhenAll(Enumerable.Range(1, 25).Select(async i =>
        {
            await using var db = new HotelDbContext(opciones);
            var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System));
            await servicio.AgregarTurnoAsync(new TurnoConversacion("pruebaz", RolConversacion.Asistente, $"respuesta {i}", DateTimeOffset.UtcNow));
        }));
        await using (var db = new HotelDbContext(opciones))
        {
            var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System));
            Console.WriteLine($"   quedan {(await servicio.ObtenerUltimosAsync("pruebaz", 100)).Count} turnos (esperado 20)");
        }

        await LimpiarAsync(opciones, huespedes);
        Console.WriteLine();
    }

    private static async Task LimpiarAsync(DbContextOptions<HotelDbContext> opciones, string[] huespedes)
    {
        await using var db = new HotelDbContext(opciones);
        var almacen = new AlmacenMemoriaClaveValorPostgres(db, TimeProvider.System);
        foreach (var id in huespedes)
        {
            await almacen.EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped(id));
        }
    }
}
