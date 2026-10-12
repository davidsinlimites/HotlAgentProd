using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Infrastructure.Memoria;

namespace HotelAgentIA.Tests.Memoria;

public class AlmacenEnMemoriaTests
{
    [Fact]
    public async Task Guarda_lee_sobrescribe_y_elimina()
    {
        var almacen = new AlmacenMemoriaClaveValorEnMemoria();

        Assert.Null(await almacen.ObtenerAsync("clave"));
        await almacen.GuardarAsync("clave", "a");
        await almacen.GuardarAsync("clave", "b");
        Assert.Equal("b", await almacen.ObtenerAsync("clave"));

        await almacen.EliminarAsync("clave");
        Assert.Null(await almacen.ObtenerAsync("clave"));
    }

    [Fact]
    public async Task El_borrado_por_prefijo_solo_afecta_a_ese_huesped()
    {
        var almacen = new AlmacenMemoriaClaveValorEnMemoria();
        foreach (var id in new[] { "30", "305", "3050" })
        {
            await almacen.GuardarAsync(ClavesMemoria.Preferencia(id, "dieta"), "x");
            await almacen.GuardarAsync(ClavesMemoria.Conversacion(id), "[]");
        }

        await almacen.EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped("305"));

        // Se compara como conjunto: el orden de las claves no importa
        Assert.Equal(
            new HashSet<string> { "huesped:30:conversacion", "huesped:30:preferencia:dieta", "huesped:3050:conversacion", "huesped:3050:preferencia:dieta" },
            almacen.Claves.ToHashSet(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Rechaza_un_prefijo_vacio(string prefijo)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            new AlmacenMemoriaClaveValorEnMemoria().EliminarPorPrefijoAsync(prefijo));
    }

    [Fact]
    public async Task ActualizarAsync_recibe_null_si_no_existe_y_despues_el_valor_actual()
    {
        var almacen = new AlmacenMemoriaClaveValorEnMemoria();
        var vistos = new List<string?>();

        await almacen.ActualizarAsync("k", actual => { vistos.Add(actual); return "uno"; });
        await almacen.ActualizarAsync("k", actual => { vistos.Add(actual); return actual + "+dos"; });

        Assert.Equal([null, "uno"], vistos);
        Assert.Equal("uno+dos", await almacen.ObtenerAsync("k"));
    }
}
