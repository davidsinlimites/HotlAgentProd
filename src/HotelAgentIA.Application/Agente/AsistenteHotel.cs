using HotelAgentIA.Application.Hotel;
using HotelAgentIA.Application.Memoria;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HotelAgentIA.Application.Agente;

/// <summary>
/// Fabrica del agente: ensambla cliente de chat, instrucciones y proveedores UNA vez (es seguro
/// compartirlo entre peticiones) y por cada peticion crea la sesion efimera del huesped y sus
/// herramientas. El huesped viene siempre del token, nunca del modelo.
/// </summary>
public sealed class AsistenteHotel
{
    private readonly AIAgent _agente;
    private readonly IServicioInfoHotel _info;
    private readonly ServicioPreferencias _preferencias;

    public AsistenteHotel(
        IChatClient cliente,
        IServicioInfoHotel info,
        ServicioConversacion conversacion,
        ServicioPreferencias preferencias,
        TimeProvider reloj)
    {
        _info = info;
        _preferencias = preferencias;
        _agente = cliente.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "AsistenteHotel",
            ChatOptions = new ChatOptions { Instructions = InstruccionesAgente.Sistema },
            ChatHistoryProvider = new HistorialConversacionProvider(conversacion, reloj),
            AIContextProviders = [new PreferenciasHuespedProvider(preferencias)]
        });
    }

    public async Task<string> ResponderAsync(string huespedId, string mensaje, CancellationToken ct = default)
    {
        var (sesion, opciones) = await PrepararAsync(huespedId, ct);
        var respuesta = await _agente.RunAsync(mensaje, sesion, opciones, ct);
        return respuesta.Text;
    }

    public async IAsyncEnumerable<string> ResponderEnStreamingAsync(
        string huespedId, string mensaje, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var (sesion, opciones) = await PrepararAsync(huespedId, ct);
        await foreach (var parte in _agente.RunStreamingAsync(mensaje, sesion, opciones, ct))
        {
            if (!string.IsNullOrEmpty(parte.Text))
            {
                yield return parte.Text;
            }
        }
    }

    private async Task<(AgentSession, AgentRunOptions)> PrepararAsync(string huespedId, CancellationToken ct)
    {
        var sesion = await SesionHuesped.CrearAsync(_agente, huespedId, ct);
        var herramientas = new HerramientasHotel(_info, huespedId).ObtenerHerramientas()
            .Concat(new HerramientasPreferencias(_preferencias, huespedId).ObtenerHerramientas())
            .ToList();
        return (sesion, new ChatClientAgentRunOptions(new ChatOptions { Tools = herramientas }));
    }
}
