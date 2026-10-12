using HotelAgentIA.Domain.Hotel;

namespace HotelAgentIA.Application.Hotel;

public sealed class ServicioInfoHotel(IRepositorioInfoHotel repositorio, TimeProvider reloj) : IServicioInfoHotel
{
    public DateOnly Hoy() => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    public Task<IReadOnlyList<Restaurante>> ListarRestaurantesAsync(CancellationToken ct = default) =>
        repositorio.ObtenerRestaurantesAsync(ct);

    public async Task<IReadOnlyList<PlatoMenu>?> ObtenerMenuAsync(
        string restauranteId, string? etiquetaDietetica = null, Comida? comida = null, CancellationToken ct = default)
    {
        if (await repositorio.ObtenerRestauranteAsync(restauranteId, ct) is null)
        {
            return null;
        }

        IEnumerable<PlatoMenu> menu = await repositorio.ObtenerMenuAsync(restauranteId, ct);

        // Un plato sin comidas se sirve en cualquier franja
        if (comida is { } franja)
        {
            menu = menu.Where(p => p.Comidas.Count == 0 || p.Comidas.Contains(franja));
        }

        if (!string.IsNullOrWhiteSpace(etiquetaDietetica))
        {
            menu = menu.Where(p => p.EtiquetasDieteticas.Contains(etiquetaDietetica.Trim(), StringComparer.OrdinalIgnoreCase));
        }

        return menu.ToList();
    }

    public Task<IReadOnlyList<Actividad>> ObtenerActividadesAsync(DateOnly? fecha = null, CancellationToken ct = default) =>
        repositorio.ObtenerActividadesAsync(fecha ?? Hoy(), ct);

    public Task<Estancia?> ObtenerEstanciaAsync(string huespedId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(huespedId);
        return repositorio.ObtenerEstanciaActivaAsync(huespedId, reloj.GetUtcNow(), ct);
    }
}
