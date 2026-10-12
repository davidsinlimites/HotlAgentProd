using HotelAgentIA.Domain.Hotel;
using HotelAgentIA.Infrastructure.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace HotelAgentIA.Infrastructure.Persistencia;

public sealed class RepositorioInfoHotel(HotelDbContext db) : IRepositorioInfoHotel
{
    public async Task<IReadOnlyList<Restaurante>> ObtenerRestaurantesAsync(CancellationToken ct = default)
    {
        var entidades = await db.Restaurantes
            .AsNoTracking()
            .Include(r => r.Horarios)
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);

        return entidades.Select(AModelo).ToList();
    }

    public async Task<Restaurante?> ObtenerRestauranteAsync(string restauranteId, CancellationToken ct = default)
    {
        var entidad = await db.Restaurantes
            .AsNoTracking()
            .Include(r => r.Horarios)
            .FirstOrDefaultAsync(r => r.Id == restauranteId, ct);

        return entidad is null ? null : AModelo(entidad);
    }

    public async Task<IReadOnlyList<PlatoMenu>> ObtenerMenuAsync(string restauranteId, CancellationToken ct = default)
    {
        var entidades = await db.Platos
            .AsNoTracking()
            .Where(p => p.RestauranteId == restauranteId)
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        return entidades
            .Select(p => new PlatoMenu(p.Id, p.RestauranteId, p.Nombre, p.Descripcion, p.Precio, p.EtiquetasDieteticas))
            .ToList();
    }

    public async Task<IReadOnlyList<Actividad>> ObtenerActividadesAsync(DateOnly fecha, CancellationToken ct = default)
    {
        var dia = fecha.DayOfWeek;
        var entidades = await db.Actividades
            .AsNoTracking()
            .Where(a => a.Dia == dia)
            .OrderBy(a => a.Inicio)
            .ToListAsync(ct);

        return entidades
            .Select(a => new Actividad(a.Id, a.Nombre, a.Descripcion, a.Ubicacion, a.Dia, a.Inicio, a.Fin))
            .ToList();
    }

    public async Task<Estancia?> ObtenerEstanciaActivaAsync(string huespedId, DateTimeOffset ahora, CancellationToken ct = default)
    {
        // Npgsql solo acepta offset 0 (UTC) en timestamptz
        var ahoraUtc = ahora.ToUniversalTime();
        var entidad = await db.Estancias
            .AsNoTracking()
            .Where(e => e.HuespedId == huespedId && e.Entrada <= ahoraUtc && ahoraUtc < e.Salida)
            .OrderByDescending(e => e.Entrada)
            .FirstOrDefaultAsync(ct);

        return entidad is null
            ? null
            : new Estancia(entidad.Id, entidad.HuespedId, entidad.NumeroHabitacion, entidad.Entrada, entidad.Salida);
    }

    private static Restaurante AModelo(RestauranteEntidad r) =>
        new(r.Id, r.Nombre, r.Descripcion, r.Ubicacion,
            r.Horarios
                .OrderBy(h => h.Dia).ThenBy(h => h.Abre)
                .Select(h => new HorarioApertura(h.Dia, h.Abre, h.Cierra))
                .ToList());
}
