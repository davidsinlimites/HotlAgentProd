using HotelAgentIA.Domain.Hotel;

namespace HotelAgentIA.Tests.Hotel;

/// <summary>Reloj fijo en UTC: lunes 2026-10-12 12:00.</summary>
public sealed class RelojFijo : TimeProvider
{
    public static readonly DateTimeOffset Ahora = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    private readonly DateTimeOffset _ahora;

    public RelojFijo(DateTimeOffset? ahora = null) => _ahora = ahora ?? Ahora;

    public override DateTimeOffset GetUtcNow() => _ahora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>
/// Repositorio en memoria con los datos mínimos del hotel de ejemplo. Las estancias se calculan
/// respecto a <c>ahora</c>: 305 y 412 activas, 501 ya terminada.
/// </summary>
public sealed class RepositorioFalso : IRepositorioInfoHotel
{
    private static readonly HorarioApertura[] TodosLosDias =
        Enum.GetValues<DayOfWeek>().Select(d => new HorarioApertura(d, new TimeOnly(7, 0), new TimeOnly(22, 30))).ToArray();

    private readonly Restaurante[] _restaurantes =
    [
        new("principal", "Restaurante Principal", "Desayuno, almuerzo y cena.", "Planta baja", TodosLosDias),
        new("bar", "Bar de la Piscina", "Bebidas y snacks.", "Terraza",
            [new HorarioApertura(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(19, 0))])
    ];

    private readonly PlatoMenu[] _platos =
    [
        new("p-desayuno", "principal", "Desayuno buffet", "Fruta y pan.", 18m, ["vegetariano"], [Comida.Desayuno]),
        new("p-risotto", "principal", "Risotto de setas", "Arroz y setas.", 21m, ["vegetariano", "sin-frutos-secos"], [Comida.Almuerzo, Comida.Cena]),
        new("p-curry", "principal", "Curry de garbanzos", "Garbanzos y coco.", 19m, ["vegano", "sin-gluten"], [Comida.Almuerzo, Comida.Cena]),
        new("p-pan", "principal", "Pan de la casa", "Siempre disponible.", 3m, [], []),
        new("b-club", "bar", "Club sandwich", "Pollo y bacon.", 15m, [], [Comida.Almuerzo])
    ];

    private readonly Actividad[] _actividades =
    [
        new("a-yoga", "Yoga al amanecer", "Sesión suave.", "Jardín", DayOfWeek.Monday, new TimeOnly(7, 30), new TimeOnly(8, 30)),
        new("a-cata", "Cata de vinos", "Vinos locales.", "Bodega", DayOfWeek.Tuesday, new TimeOnly(19, 0), new TimeOnly(20, 0))
    ];

    private readonly Estancia[] _estancias;

    public RepositorioFalso(DateTimeOffset? ahora = null)
    {
        var t = ahora ?? RelojFijo.Ahora;
        _estancias =
        [
            new("e-305", "305", "305", t.AddDays(-1), t.AddDays(3)),
            new("e-412", "412", "412", t.AddDays(-2), t.AddDays(2)),
            new("e-501", "501", "501", t.AddDays(-10), t.AddDays(-5))
        ];
    }

    public Task<IReadOnlyList<Restaurante>> ObtenerRestaurantesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Restaurante>>(_restaurantes);

    public Task<Restaurante?> ObtenerRestauranteAsync(string restauranteId, CancellationToken ct = default) =>
        Task.FromResult(_restaurantes.FirstOrDefault(r => r.Id == restauranteId));

    public Task<IReadOnlyList<PlatoMenu>> ObtenerMenuAsync(string restauranteId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PlatoMenu>>(_platos.Where(p => p.RestauranteId == restauranteId).ToList());

    public Task<IReadOnlyList<Actividad>> ObtenerActividadesAsync(DateOnly fecha, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Actividad>>(_actividades.Where(a => a.Dia == fecha.DayOfWeek).ToList());

    public Task<Estancia?> ObtenerEstanciaActivaAsync(string huespedId, DateTimeOffset ahora, CancellationToken ct = default) =>
        Task.FromResult(_estancias.FirstOrDefault(e => e.HuespedId == huespedId && e.EstaActivaEn(ahora)));
}
