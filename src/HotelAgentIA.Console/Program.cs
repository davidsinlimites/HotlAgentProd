using System.Text;
using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Domain.Memoria;
using HotelAgentIA.Infrastructure.Memoria;

Console.OutputEncoding = Encoding.UTF8;

var almacen = new AlmacenMemoriaClaveValorEnMemoria();
var preferencias = new ServicioPreferencias(almacen);
var conversacion = new ServicioConversacion(almacen);

// Dos huéspedes guardan preferencias y conversación
await preferencias.GuardarAsync("305", "dieta", "vegetariano");
await preferencias.GuardarAsync("412", "dieta", "sin gluten");

await conversacion.AgregarTurnoAsync(new TurnoConversacion(
    "305", RolConversacion.Usuario, "¿A qué hora es el desayuno?", DateTimeOffset.Now));
await conversacion.AgregarTurnoAsync(new TurnoConversacion(
    "305", RolConversacion.Asistente, "De 7:00 a 10:00.", DateTimeOffset.Now));
await conversacion.AgregarTurnoAsync(new TurnoConversacion(
    "412", RolConversacion.Usuario, "¿Hay actividades hoy?", DateTimeOffset.Now));

await MostrarEstadoAsync("Estado inicial");

// Checkout del 305: borra solo lo suyo
await almacen.EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped("305"));

await MostrarEstadoAsync("Tras el checkout del 305");

async Task MostrarEstadoAsync(string titulo)
{
    Console.WriteLine($"=== {titulo} ===");

    Console.WriteLine("Claves en el almacén:");
    foreach (var clave in almacen.Claves.Order(StringComparer.Ordinal))
    {
        Console.WriteLine($"  {clave}");
    }

    foreach (var id in new[] { "305", "412" })
    {
        var dieta = await preferencias.ObtenerAsync(id, "dieta");
        var turnos = await conversacion.ObtenerUltimosAsync(id, 10);

        Console.WriteLine($"Huésped {id}: dieta = {dieta ?? "(nada)"}, turnos = {turnos.Count}");
        foreach (var turno in turnos)
        {
            Console.WriteLine($"    [{turno.Rol}] {turno.Contenido}");
        }
    }

    Console.WriteLine();
}