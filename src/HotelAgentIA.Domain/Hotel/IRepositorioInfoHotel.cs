namespace HotelAgentIA.Domain.Hotel;

public interface IRepositorioInfoHotel
{
    Task<IReadOnlyList<Restaurante>> ObtenerRestaurantesAsync(CancellationToken ct = default);
    Task<Restaurante?> ObtenerRestauranteAsync(string restauranteId, CancellationToken ct = default);
    Task<IReadOnlyList<PlatoMenu>> ObtenerMenuAsync(string restauranteId, CancellationToken ct = default);
    Task<IReadOnlyList<Actividad>> ObtenerActividadesAsync(DateOnly fecha, CancellationToken ct = default);
    Task<Estancia?> ObtenerEstanciaActivaAsync(string huespedId, DateTimeOffset ahora, CancellationToken ct = default);
}
