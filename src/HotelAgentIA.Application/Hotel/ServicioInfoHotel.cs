using HotelAgentIA.Domain.Hotel;

namespace HotelAgentIA.Application.Hotel;

public sealed class ServicioInfoHotel(IRepositorioInfoHotel repositorio, TimeProvider reloj) : IServicioInfoHotel
{
    public DateOnly Hoy() => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    public Task<IReadOnlyList<Restaurante>> ListarRestaurantesAsync(CancellationToken ct = default) =>
        repositorio.ObtenerRestaurantesAsync(ct);

    public async Task<IReadOnlyList<PlatoMenu>?> ObtenerMenuAsync(
        string restauranteId, string? etiquetaDietetica = null, CancellationToken ct = default)
    {
        if (await repositorio.ObtenerRestauranteAsync(restauranteId, ct) is null)
        {
            return null;
        }

        var menu = await repositorio.ObtenerMenuAsync(restauranteId, ct);
        if (string.IsNullOrWhiteSpace(etiquetaDietetica))
        {
            return menu;
        }

        return menu
            .Where(p => p.EtiquetasDieteticas.Contains(etiquetaDietetica.Trim(), StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public Task<IReadOnlyList<Actividad>> ObtenerActividadesAsync(DateOnly? fecha = null, CancellationToken ct = default) =>
        repositorio.ObtenerActividadesAsync(fecha ?? Hoy(), ct);

    public Task<Estancia?> ObtenerEstanciaAsync(string huespedId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(huespedId);
        return repositorio.ObtenerEstanciaActivaAsync(huespedId, reloj.GetUtcNow(), ct);
    }
}
