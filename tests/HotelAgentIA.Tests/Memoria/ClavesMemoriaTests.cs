using HotelAgentIA.Application.Memoria;

namespace HotelAgentIA.Tests.Memoria;

public class ClavesMemoriaTests
{
    [Fact]
    public void Las_claves_tienen_el_formato_esperado()
    {
        Assert.Equal("huesped:305:", ClavesMemoria.PrefijoHuesped("305"));
        Assert.Equal("huesped:305:preferencia:dieta", ClavesMemoria.Preferencia("305", "dieta"));
        Assert.Equal("huesped:305:conversacion", ClavesMemoria.Conversacion("305"));
    }

    [Fact]
    public void El_prefijo_de_un_huesped_no_es_prefijo_de_otro()
    {
        var clave305 = ClavesMemoria.Conversacion("305");

        Assert.DoesNotMatch("^" + System.Text.RegularExpressions.Regex.Escape(ClavesMemoria.PrefijoHuesped("30")), clave305);
        Assert.DoesNotMatch("^" + System.Text.RegularExpressions.Regex.Escape(ClavesMemoria.PrefijoHuesped("3050")), clave305);
        Assert.StartsWith(ClavesMemoria.PrefijoHuesped("305"), clave305, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("30:5")]
    [InlineData("huesped:412:dieta")]
    public void Rechaza_ids_vacios_o_con_dos_puntos(string huespedId)
    {
        Assert.ThrowsAny<ArgumentException>(() => ClavesMemoria.PrefijoHuesped(huespedId));
        Assert.ThrowsAny<ArgumentException>(() => ClavesMemoria.Conversacion(huespedId));
        Assert.ThrowsAny<ArgumentException>(() => ClavesMemoria.Preferencia(huespedId, "dieta"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a:b")]
    public void Rechaza_nombres_de_preferencia_invalidos(string nombre)
    {
        Assert.ThrowsAny<ArgumentException>(() => ClavesMemoria.Preferencia("305", nombre));
    }
}
