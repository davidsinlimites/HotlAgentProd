using DotNetEnv;
using HotelAgentIA.Application.Agente;
using HotelAgentIA.Application.Hotel;
using HotelAgentIA.Infrastructure.Chat;
using HotelAgentIA.Infrastructure.Persistencia;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

/// <summary>
/// Banco de pruebas de las herramientas del agente: datos reales de PostgreSQL y un modelo falso que
/// llama a las herramientas. Comprueba que el huesped sale de quien construye las herramientas
/// (por peticion) y que el modelo no puede elegirlo.
/// </summary>
internal static class PruebaHerramientas
{
    public static async Task EjecutarAsync()
    {
        Console.WriteLine("=== Herramientas del agente ===");

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
        var servicio = new ServicioInfoHotel(new RepositorioInfoHotel(db), TimeProvider.System);

        Console.WriteLine("-- Herramientas expuestas al modelo (huesped 305)");
        foreach (var herramienta in new HerramientasHotel(servicio, "305").ObtenerHerramientas().OfType<AIFunction>())
        {
            var parametros = herramienta.JsonSchema.TryGetProperty("properties", out var props)
                ? string.Join(", ", props.EnumerateObject().Select(p => p.Name))
                : "";
            Console.WriteLine($"   {herramienta.Name}({parametros})");
        }

        // Agente unico; las herramientas se pasan por peticion con el huesped de esa peticion
        var agente = ClienteChatFalso.ConLlamadaAHerramienta("consultar_mi_estancia")
            .AsAIAgent(instructions: "Eres el asistente de un hotel.", name: "AsistenteHotel");

        Console.WriteLine("-- consultar_mi_estancia, misma instancia de agente, huespedes distintos");
        foreach (var huesped in new[] { "305", "412", "999" })
        {
            var respuesta = await EjecutarComoAsync(agente, servicio, huesped);
            Console.WriteLine($"   huesped {huesped}: {respuesta}");
        }

        Console.WriteLine("-- Sin herramientas por peticion");
        Console.WriteLine($"   {(await agente.RunAsync("¿Cuál es mi habitación?")).Text}");

        Console.WriteLine("-- consultar_menu con filtro (el modelo elige restaurante y etiqueta)");
        var agenteMenu = ClienteChatFalso
            .ConLlamadaAHerramienta("consultar_menu", new Dictionary<string, object?> { ["restauranteId"] = "principal", ["etiquetaDietetica"] = "vegano" })
            .AsAIAgent(instructions: "Eres el asistente de un hotel.");
        Console.WriteLine($"   {await EjecutarComoAsync(agenteMenu, servicio, "305")}");

        Console.WriteLine("-- consultar_menu por comida: cena no debe incluir el desayuno; una comida invalida se explica");
        foreach (var comida in new[] { "cena", "desayuno", "brunch" })
        {
            var agenteComida = ClienteChatFalso
                .ConLlamadaAHerramienta("consultar_menu", new Dictionary<string, object?> { ["restauranteId"] = "principal", ["comida"] = comida })
                .AsAIAgent(instructions: "x");
            Console.WriteLine($"   {comida}: {(await EjecutarComoAsync(agenteComida, servicio, "305")).ReplaceLineEndings(" | ")}");
        }

        Console.WriteLine("-- consultar_restaurantes y consultar_actividades");
        var agenteRestaurantes = ClienteChatFalso.ConLlamadaAHerramienta("consultar_restaurantes").AsAIAgent(instructions: "x");
        Console.WriteLine($"   {await EjecutarComoAsync(agenteRestaurantes, servicio, "305")}");
        var agenteActividades = ClienteChatFalso.ConLlamadaAHerramienta("consultar_actividades").AsAIAgent(instructions: "x");
        Console.WriteLine($"   {await EjecutarComoAsync(agenteActividades, servicio, "305")}");

        Console.WriteLine();
    }

    private static async Task<string> EjecutarComoAsync(AIAgent agente, IServicioInfoHotel servicio, string huespedId)
    {
        var herramientas = new HerramientasHotel(servicio, huespedId).ObtenerHerramientas();
        var opcionesDeEjecucion = new ChatClientAgentRunOptions(new ChatOptions { Tools = herramientas });
        var respuesta = await agente.RunAsync("Consulta lo que haga falta.", options: opcionesDeEjecucion);
        return respuesta.Text;
    }
}
