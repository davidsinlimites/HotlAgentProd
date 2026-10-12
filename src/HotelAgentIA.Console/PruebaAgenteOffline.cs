using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using HotelAgentIA.Infrastructure.Chat;

/// <summary>
/// Banco de pruebas del agente con el cliente falso: sin red ni costo.
/// Comprueba RunAsync, RunStreamingAsync y que la sesion conserva el historial.
/// </summary>
internal static class PruebaAgenteOffline
{
    public static async Task EjecutarAsync()
    {
        Console.WriteLine("=== Agente con cliente falso ===");

        // Responde con el numero de mensajes que recibe: asi se ve si la sesion acumula historial
        using var cliente = new ClienteChatFalso(
            mensajes => $"[modo offline] Recibi {mensajes.Count} mensajes; el ultimo del usuario: {mensajes.Last(m => m.Role == ChatRole.User).Text}",
            TimeSpan.FromMilliseconds(80));

        var agente = cliente.AsAIAgent(
            instructions: "Eres el asistente de un hotel.",
            name: "AsistenteHotel");

        var sesion = await agente.CreateSessionAsync();

        Console.WriteLine("-- RunAsync (respuesta completa)");
        var respuesta = await agente.RunAsync("Hola, ¿a qué hora es el desayuno?", sesion);
        Console.WriteLine(respuesta.Text);

        Console.WriteLine("-- RunStreamingAsync (palabra por palabra), misma sesion");
        await foreach (var fragmento in agente.RunStreamingAsync("¿Y la cena?", sesion))
        {
            Console.Write(fragmento.Text);
        }

        Console.WriteLine();
        Console.WriteLine();
    }
}
