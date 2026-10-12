using HotelAgentIA.Domain.Hotel;

namespace HotelAgentIA.Application.Hotel;

/// <summary>
/// Informacion del hotel de solo lectura para el agente. El huesped siempre llega ya resuelto
/// desde el token por quien construye las herramientas; el modelo nunca lo elige.
/// </summary>
public interface IServicioInfoHotel
{
    /// <summary>Fecha actual del hotel.</summary>
    DateOnly Hoy();

    Task<IReadOnlyList<Restaurante>> ListarRestaurantesAsync(CancellationToken ct = default);

    /// <summary>Devuelve null si el restaurante no existe. La etiqueta (p. ej. "vegano") y la comida (desayuno, almuerzo, cena) filtran los platos; un plato sin comidas sirve para cualquiera.</summary>
    Task<IReadOnlyList<PlatoMenu>?> ObtenerMenuAsync(string restauranteId, string? etiquetaDietetica = null, Comida? comida = null, CancellationToken ct = default);

    /// <summary>Sin fecha, las de hoy.</summary>
    Task<IReadOnlyList<Actividad>> ObtenerActividadesAsync(DateOnly? fecha = null, CancellationToken ct = default);

    /// <summary>Estancia activa del huesped indicado, o null si no tiene.</summary>
    Task<Estancia?> ObtenerEstanciaAsync(string huespedId, CancellationToken ct = default);
}
