using Microsoft.Agents.AI;

namespace HotelAgentIA.Application.Agente;

/// <summary>
/// Une una AgentSession con el huesped del token. Los proveedores de historial y de contexto
/// son una sola instancia compartida por todas las sesiones, asi que el huesped vive en el
/// StateBag de la sesion y nunca en un campo del proveedor. La sesion es efimera: se crea por
/// peticion y no se serializa; la memoria durable esta en el almacen, por huesped.
/// </summary>
public static class SesionHuesped
{
    private const string Clave = "huesped-id";

    public static async Task<AgentSession> CrearAsync(AIAgent agente, string huespedId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(agente);
        ArgumentException.ThrowIfNullOrWhiteSpace(huespedId);

        var sesion = await agente.CreateSessionAsync(ct);
        sesion.StateBag.SetValue(Clave, huespedId);
        return sesion;
    }

    public static string ObtenerHuespedId(AgentSession? sesion)
    {
        if (sesion is not null
            && sesion.StateBag.TryGetValue<string>(Clave, out var id)
            && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        throw new InvalidOperationException(
            "La sesion no tiene huesped. Crea la sesion con SesionHuesped.CrearAsync.");
    }
}
