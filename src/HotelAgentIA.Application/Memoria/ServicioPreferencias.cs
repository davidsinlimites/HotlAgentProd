using HotelAgentIA.Domain.Memoria;

namespace HotelAgentIA.Application.Memoria;

/// <summary>
/// Guarda y lee las preferencias de un huésped (dieta, idioma, hora de desayuno...).
/// No sabe si detrás hay un diccionario o MySQL: solo conoce el contrato de Domain.
/// </summary>
public sealed class ServicioPreferencias
{
    private readonly IAlmacenMemoriaClaveValor _almacen;

    public ServicioPreferencias(IAlmacenMemoriaClaveValor almacen)
    {
        _almacen = almacen;
    }

    // Devuelve null si el huésped no tiene esa preferencia.
    public Task<string?> ObtenerAsync(string huespedId, string nombre, CancellationToken ct = default)
    {
        return _almacen.ObtenerAsync(ClavesMemoria.Preferencia(huespedId, nombre), ct);
    }

    public Task GuardarAsync(string huespedId, string nombre, string valor, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        return _almacen.GuardarAsync(ClavesMemoria.Preferencia(huespedId, nombre), valor, ct);
    }
}