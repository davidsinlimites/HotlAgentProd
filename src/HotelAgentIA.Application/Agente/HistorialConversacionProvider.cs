using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Domain.Memoria;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HotelAgentIA.Application.Agente;

/// <summary>
/// Historial de la conversacion del huesped sobre ServicioConversacion (una conversacion por
/// huesped, maximo 20 turnos). Solo guarda turnos de texto del usuario y del asistente: las
/// llamadas a herramientas y sus resultados no se guardan, porque pueden quedar desactualizados.
/// </summary>
public sealed class HistorialConversacionProvider(ServicioConversacion conversacion, TimeProvider reloj) : ChatHistoryProvider
{
    private const int MaximoTurnosCargados = 20;

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var huespedId = SesionHuesped.ObtenerHuespedId(context.Session);
        var turnos = await conversacion.ObtenerUltimosAsync(huespedId, MaximoTurnosCargados, cancellationToken);

        return turnos.Select(t => new ChatMessage(
            t.Rol == RolConversacion.Usuario ? ChatRole.User : ChatRole.Assistant,
            t.Contenido));
    }

    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context, CancellationToken cancellationToken = default)
    {
        var huespedId = SesionHuesped.ObtenerHuespedId(context.Session);

        // Solo lo que escribio la persona (no el historial ni el contexto que anadieron los proveedores)
        var textoUsuario = context.RequestMessages
            .Where(m => m.Role == ChatRole.User
                        && m.GetAgentRequestMessageSourceType() == AgentRequestMessageSourceType.External
                        && !string.IsNullOrWhiteSpace(m.Text))
            .Select(m => m.Text);

        // Solo el texto que el asistente dijo al huesped (se descartan llamadas y resultados de herramientas)
        var textoAsistente = (context.ResponseMessages ?? [])
            .Where(m => m.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(m.Text))
            .Select(m => m.Text)
            .ToList();

        foreach (var texto in textoUsuario)
        {
            await conversacion.AgregarTurnoAsync(
                new TurnoConversacion(huespedId, RolConversacion.Usuario, texto, reloj.GetUtcNow()), cancellationToken);
        }

        if (textoAsistente.Count > 0)
        {
            await conversacion.AgregarTurnoAsync(
                new TurnoConversacion(huespedId, RolConversacion.Asistente, string.Join("\n", textoAsistente), reloj.GetUtcNow()),
                cancellationToken);
        }
    }
}
