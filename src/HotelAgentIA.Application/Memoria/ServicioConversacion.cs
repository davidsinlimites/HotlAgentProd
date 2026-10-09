using System.Text.Json;
using System.Text.Json.Serialization;
using HotelAgentIA.Domain.Memoria;

namespace HotelAgentIA.Application.Memoria;

/// <summary>
/// Guarda el hilo reciente de la conversación de cada huésped.
/// Todos los turnos de un huésped viven en UNA clave, como lista JSON, porque el
/// contrato del almacén no permite listar claves por prefijo.
/// Limitación conocida: leer-modificar-escribir no es atómico; dos mensajes
/// simultáneos del mismo huésped podrían pisarse.
/// </summary>
public sealed class ServicioConversacion
{
    private const int MaximoTurnosGuardados = 20;

    // Guarda el rol como "Usuario"/"Asistente" en vez de 0/1, para poder leerlo al depurar.
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IAlmacenMemoriaClaveValor _almacen;

    public ServicioConversacion(IAlmacenMemoriaClaveValor almacen)
    {
        _almacen = almacen;
    }

    public async Task AgregarTurnoAsync(TurnoConversacion turno, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(turno);

        var clave = ClavesMemoria.Conversacion(turno.HuespedId);
        var turnos = await LeerAsync(clave, ct);

        turnos.Add(turno);

        if (turnos.Count > MaximoTurnosGuardados)
        {
            turnos.RemoveRange(0, turnos.Count - MaximoTurnosGuardados);
        }

        await _almacen.GuardarAsync(clave, JsonSerializer.Serialize(turnos, OpcionesJson), ct);
    }

    // Devuelve los últimos "maximo" turnos, del más antiguo al más reciente.
    public async Task<IReadOnlyList<TurnoConversacion>> ObtenerUltimosAsync(
        string huespedId, int maximo, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximo);

        var turnos = await LeerAsync(ClavesMemoria.Conversacion(huespedId), ct);

        return turnos.Count <= maximo
            ? turnos
            : turnos.GetRange(turnos.Count - maximo, maximo);
    }

    private async Task<List<TurnoConversacion>> LeerAsync(string clave, CancellationToken ct)
    {
        var json = await _almacen.ObtenerAsync(clave, ct);

        if (json is null)
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<TurnoConversacion>>(json, OpcionesJson) ?? [];
    }
}