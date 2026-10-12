using System.ComponentModel;
using System.Globalization;
using HotelAgentIA.Application.Hotel;
using HotelAgentIA.Domain.Hotel;
using Microsoft.Extensions.AI;

namespace HotelAgentIA.Application.Agente;

/// <summary>
/// Herramientas de solo lectura del agente. Se construye UNA POR PETICION con el huesped ya
/// resuelto del token: ninguna herramienta tiene un parametro de huesped, asi que el modelo
/// no puede pedir datos de otra persona.
/// </summary>
public sealed class HerramientasHotel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly IServicioInfoHotel _servicio;
    private readonly string _huespedId;

    public HerramientasHotel(IServicioInfoHotel servicio, string huespedId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(huespedId);
        _servicio = servicio;
        _huespedId = huespedId;
    }

    public IList<AITool> ObtenerHerramientas() =>
    [
        AIFunctionFactory.Create(ConsultarRestaurantes, "consultar_restaurantes"),
        AIFunctionFactory.Create(ConsultarMenu, "consultar_menu"),
        AIFunctionFactory.Create(ConsultarActividades, "consultar_actividades"),
        AIFunctionFactory.Create(ConsultarMiEstancia, "consultar_mi_estancia")
    ];

    [Description("Lista los restaurantes y bares del hotel con su identificador, ubicacion y horario de hoy.")]
    private async Task<string> ConsultarRestaurantes(CancellationToken ct)
    {
        var restaurantes = await _servicio.ListarRestaurantesAsync(ct);
        var hoy = _servicio.Hoy().DayOfWeek;

        return string.Join("\n", restaurantes.Select(r =>
        {
            var horario = r.Horarios.FirstOrDefault(h => h.Dia == hoy);
            var textoHorario = horario is null
                ? "cerrado hoy"
                : $"hoy {horario.Abre.ToString("HH\\:mm", Cultura)}-{horario.Cierra.ToString("HH\\:mm", Cultura)}";
            return $"- {r.Id}: {r.Nombre} ({r.Ubicacion}). {r.Descripcion} {textoHorario}.";
        }));
    }

    [Description("Muestra el menu de un restaurante. Opcionalmente filtra por etiqueta dietetica (vegano, vegetariano, sin-gluten) y por comida (desayuno, almuerzo, cena). Si el huesped pregunta por una comida concreta, indica la comida.")]
    private async Task<string> ConsultarMenu(
        [Description("Identificador del restaurante, tal como lo devuelve consultar_restaurantes.")] string restauranteId,
        [Description("Etiqueta dietetica para filtrar. Omitir para ver todo el menu.")] string? etiquetaDietetica = null,
        [Description("Comida: desayuno, almuerzo o cena. Omitir para ver todas.")] string? comida = null,
        CancellationToken ct = default)
    {
        Comida? franja = null;
        if (!string.IsNullOrWhiteSpace(comida))
        {
            if (!Enum.TryParse<Comida>(comida.Trim(), ignoreCase: true, out var valida) || !Enum.IsDefined(valida))
            {
                return $"La comida '{comida}' no es valida; usa desayuno, almuerzo o cena.";
            }

            franja = valida;
        }

        var menu = await _servicio.ObtenerMenuAsync(restauranteId, etiquetaDietetica, franja, ct);
        if (menu is null)
        {
            return $"No existe el restaurante '{restauranteId}'. Usa consultar_restaurantes para ver los identificadores validos.";
        }

        if (menu.Count == 0)
        {
            return "No hay platos que coincidan.";
        }

        return string.Join("\n", menu.Select(p =>
        {
            var etiquetas = p.EtiquetasDieteticas.Count == 0 ? "" : $" [{string.Join(", ", p.EtiquetasDieteticas)}]";
            var comidas = p.Comidas.Count == 0 ? "" : $" Se sirve en: {string.Join(", ", p.Comidas.Select(c => c.ToString().ToLowerInvariant()))}.";
            return $"- {p.Nombre}: {p.Descripcion} Precio {p.Precio.ToString("0.00", Cultura)}.{etiquetas}{comidas}";
        }));
    }

    [Description("Lista las actividades del hotel de un dia. Sin fecha, las de hoy.")]
    private async Task<string> ConsultarActividades(
        [Description("Fecha en formato AAAA-MM-DD. Omitir para hoy.")] string? fecha = null,
        CancellationToken ct = default)
    {
        DateOnly? dia = null;
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", Cultura, DateTimeStyles.None, out var valida))
            {
                return $"La fecha '{fecha}' no es valida; usa el formato AAAA-MM-DD.";
            }

            dia = valida;
        }

        var actividades = await _servicio.ObtenerActividadesAsync(dia, ct);
        if (actividades.Count == 0)
        {
            return "No hay actividades ese dia.";
        }

        return string.Join("\n", actividades.Select(a =>
            $"- {a.Inicio.ToString("HH\\:mm", Cultura)}-{a.Fin.ToString("HH\\:mm", Cultura)} {a.Nombre} ({a.Ubicacion}). {a.Descripcion}"));
    }

    [Description("Devuelve la habitacion y las fechas de entrada y salida de la estancia del huesped que esta conversando.")]
    private async Task<string> ConsultarMiEstancia(CancellationToken ct)
    {
        var estancia = await _servicio.ObtenerEstanciaAsync(_huespedId, ct);
        if (estancia is null)
        {
            return "No hay una estancia activa para este huesped.";
        }

        return $"Habitacion {estancia.NumeroHabitacion}. Entrada {estancia.Entrada.ToLocalTime().ToString("yyyy-MM-dd HH:mm", Cultura)}, " +
               $"salida {estancia.Salida.ToLocalTime().ToString("yyyy-MM-dd HH:mm", Cultura)}.";
    }
}
