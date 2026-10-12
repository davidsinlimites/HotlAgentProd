using HotelAgentIA.Application.Hotel;
using HotelAgentIA.Domain.Hotel;

namespace HotelAgentIA.Tests.Hotel;

public class ServicioInfoHotelTests
{
    private static ServicioInfoHotel Crear() => new(new RepositorioFalso(), new RelojFijo());

    [Fact]
    public void Hoy_usa_el_reloj_inyectado()
    {
        Assert.Equal(new DateOnly(2026, 10, 12), Crear().Hoy());
    }

    [Fact]
    public async Task Menu_de_un_restaurante_inexistente_es_null()
    {
        Assert.Null(await Crear().ObtenerMenuAsync("no-existe"));
    }

    [Fact]
    public async Task Sin_filtros_devuelve_todo_el_menu()
    {
        var menu = await Crear().ObtenerMenuAsync("principal");

        Assert.Equal(4, menu!.Count);
    }

    [Fact]
    public async Task Filtra_por_etiqueta_sin_distinguir_mayusculas_ni_espacios()
    {
        var menu = await Crear().ObtenerMenuAsync("principal", "  VEGANO ");

        Assert.Equal(["Curry de garbanzos"], menu!.Select(p => p.Nombre));
    }

    [Fact]
    public async Task La_cena_no_incluye_el_desayuno_pero_si_los_platos_sin_franja()
    {
        var menu = await Crear().ObtenerMenuAsync("principal", comida: Comida.Cena);

        Assert.DoesNotContain(menu!, p => p.Nombre == "Desayuno buffet");
        Assert.Contains(menu!, p => p.Nombre == "Pan de la casa");
        Assert.Contains(menu!, p => p.Nombre == "Risotto de setas");
    }

    [Fact]
    public async Task El_desayuno_solo_incluye_platos_de_desayuno_o_sin_franja()
    {
        var menu = await Crear().ObtenerMenuAsync("principal", comida: Comida.Desayuno);

        Assert.Equal(["Desayuno buffet", "Pan de la casa"], menu!.Select(p => p.Nombre).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Combina_etiqueta_y_comida()
    {
        var menu = await Crear().ObtenerMenuAsync("principal", "vegetariano", Comida.Cena);

        Assert.Equal(["Risotto de setas"], menu!.Select(p => p.Nombre));
    }

    [Fact]
    public async Task Las_actividades_por_defecto_son_las_de_hoy()
    {
        var hoy = await Crear().ObtenerActividadesAsync();
        var martes = await Crear().ObtenerActividadesAsync(new DateOnly(2026, 10, 13));

        Assert.Equal(["Yoga al amanecer"], hoy.Select(a => a.Nombre));
        Assert.Equal(["Cata de vinos"], martes.Select(a => a.Nombre));
    }

    [Theory]
    [InlineData("305", "305")]
    [InlineData("412", "412")]
    public async Task Devuelve_la_estancia_activa_del_huesped(string huespedId, string habitacion)
    {
        var estancia = await Crear().ObtenerEstanciaAsync(huespedId);

        Assert.Equal(habitacion, estancia!.NumeroHabitacion);
    }

    [Theory]
    [InlineData("999")]
    [InlineData("501")]
    public async Task Sin_estancia_activa_devuelve_null(string huespedId)
    {
        Assert.Null(await Crear().ObtenerEstanciaAsync(huespedId));
    }

    [Fact]
    public async Task Rechaza_un_huesped_vacio()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => Crear().ObtenerEstanciaAsync(" "));
    }
}
