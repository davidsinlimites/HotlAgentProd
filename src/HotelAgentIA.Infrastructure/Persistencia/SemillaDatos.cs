using HotelAgentIA.Infrastructure.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace HotelAgentIA.Infrastructure.Persistencia;

/// <summary>Datos de ejemplo para desarrollo y la demo. Idempotente: si ya hay restaurantes, no hace nada.</summary>
public static class SemillaDatos
{
    public static async Task AplicarAsync(HotelDbContext db, DateTimeOffset ahora, CancellationToken ct = default)
    {
        if (await db.Restaurantes.AnyAsync(ct))
        {
            return;
        }

        var todosLosDias = Enum.GetValues<DayOfWeek>();

        db.Restaurantes.AddRange(
            new RestauranteEntidad
            {
                Id = "principal",
                Nombre = "Restaurante Principal",
                Descripcion = "Desayuno buffet, almuerzo y cena a la carta.",
                Ubicacion = "Planta baja",
                Horarios = todosLosDias.Select(d => Horario(d, 7, 0, 22, 30)).ToList(),
                Platos =
                [
                    Plato("principal-desayuno", "Desayuno buffet", "Fruta, panes, huevos, cafe y jugos.", 18m, "vegetariano"),
                    Plato("principal-risotto", "Risotto de setas", "Arroz arborio, setas y parmesano.", 21m, "vegetariano", "sin-frutos-secos"),
                    Plato("principal-salmon", "Salmon a la plancha", "Con verduras asadas y pure.", 29m, "sin-gluten"),
                    Plato("principal-curry", "Curry de garbanzos", "Garbanzos, coco y especias con arroz basmati.", 19m, "vegano", "sin-gluten")
                ]
            },
            new RestauranteEntidad
            {
                Id = "bar-piscina",
                Nombre = "Bar de la Piscina",
                Descripcion = "Ligeros, bebidas y cocteles junto a la piscina.",
                Ubicacion = "Terraza, planta 2",
                Horarios = todosLosDias.Select(d => Horario(d, 11, 0, 19, 0)).ToList(),
                Platos =
                [
                    Plato("bar-ensalada", "Ensalada fresca", "Hojas verdes, tomate, aguacate y vinagreta.", 12m, "vegano", "sin-gluten"),
                    Plato("bar-club", "Club sandwich", "Pollo, bacon, huevo y patatas fritas.", 15m)
                ]
            });

        db.Actividades.AddRange(
            Actividad("yoga-lunes", "Yoga al amanecer", "Sesion suave para todos los niveles.", "Jardin", DayOfWeek.Monday, 7, 30, 8, 30),
            Actividad("yoga-miercoles", "Yoga al amanecer", "Sesion suave para todos los niveles.", "Jardin", DayOfWeek.Wednesday, 7, 30, 8, 30),
            Actividad("aquagym", "Aquagym", "Ejercicio en la piscina con monitor.", "Piscina", DayOfWeek.Tuesday, 10, 0, 11, 0),
            Actividad("cata-vinos", "Cata de vinos", "Degustacion guiada de tres vinos locales.", "Bodega", DayOfWeek.Friday, 18, 30, 20, 0),
            Actividad("cine-aire-libre", "Cine al aire libre", "Pelicula bajo las estrellas.", "Terraza", DayOfWeek.Saturday, 21, 0, 23, 0),
            Actividad("brunch-musical", "Brunch con musica en vivo", "Brunch con musica en directo.", "Restaurante Principal", DayOfWeek.Sunday, 11, 0, 14, 0));

        var ahoraUtc = ahora.ToUniversalTime();
        db.Huespedes.AddRange(
            new HuespedEntidad { Id = "305", Nombre = "Huesped 305" },
            new HuespedEntidad { Id = "412", Nombre = "Huesped 412" });
        db.Estancias.AddRange(
            new EstanciaEntidad { Id = "estancia-305", HuespedId = "305", NumeroHabitacion = "305", Entrada = ahoraUtc.AddDays(-1), Salida = ahoraUtc.AddDays(3) },
            new EstanciaEntidad { Id = "estancia-412", HuespedId = "412", NumeroHabitacion = "412", Entrada = ahoraUtc.AddDays(-2), Salida = ahoraUtc.AddDays(2) });

        await db.SaveChangesAsync(ct);
    }

    // El RestauranteId lo rellena EF al asociar el horario o el plato con su restaurante
    private static HorarioAperturaEntidad Horario(DayOfWeek dia, int hAbre, int mAbre, int hCierra, int mCierra) =>
        new() { RestauranteId = "", Dia = dia, Abre = new TimeOnly(hAbre, mAbre), Cierra = new TimeOnly(hCierra, mCierra) };

    private static PlatoMenuEntidad Plato(string id, string nombre, string descripcion, decimal precio, params string[] etiquetas) =>
        new() { Id = id, RestauranteId = "", Nombre = nombre, Descripcion = descripcion, Precio = precio, EtiquetasDieteticas = etiquetas };

    private static ActividadEntidad Actividad(string id, string nombre, string descripcion, string ubicacion, DayOfWeek dia, int hIni, int mIni, int hFin, int mFin) =>
        new() { Id = id, Nombre = nombre, Descripcion = descripcion, Ubicacion = ubicacion, Dia = dia, Inicio = new TimeOnly(hIni, mIni), Fin = new TimeOnly(hFin, mFin) };
}
