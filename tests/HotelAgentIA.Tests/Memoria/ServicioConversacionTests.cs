using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Domain.Memoria;
using HotelAgentIA.Infrastructure.Memoria;

namespace HotelAgentIA.Tests.Memoria;

public class ServicioConversacionTests
{
    private static readonly DateTimeOffset Fecha = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    private static TurnoConversacion Turno(string huesped, string contenido, RolConversacion rol = RolConversacion.Usuario) =>
        new(huesped, rol, contenido, Fecha);

    [Fact]
    public async Task Devuelve_los_turnos_en_orden_del_mas_antiguo_al_mas_reciente()
    {
        var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorEnMemoria());
        await servicio.AgregarTurnoAsync(Turno("305", "uno"));
        await servicio.AgregarTurnoAsync(Turno("305", "dos", RolConversacion.Asistente));

        var turnos = await servicio.ObtenerUltimosAsync("305", 10);

        Assert.Equal(["uno", "dos"], turnos.Select(t => t.Contenido));
        Assert.Equal([RolConversacion.Usuario, RolConversacion.Asistente], turnos.Select(t => t.Rol));
    }

    [Fact]
    public async Task Solo_conserva_los_ultimos_20_turnos()
    {
        var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorEnMemoria());
        for (var i = 1; i <= 25; i++)
        {
            await servicio.AgregarTurnoAsync(Turno("305", $"m{i}"));
        }

        var turnos = await servicio.ObtenerUltimosAsync("305", 100);

        Assert.Equal(20, turnos.Count);
        Assert.Equal("m6", turnos[0].Contenido);
        Assert.Equal("m25", turnos[^1].Contenido);
    }

    [Fact]
    public async Task ObtenerUltimos_devuelve_solo_los_pedidos()
    {
        var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorEnMemoria());
        for (var i = 1; i <= 5; i++)
        {
            await servicio.AgregarTurnoAsync(Turno("305", $"m{i}"));
        }

        var turnos = await servicio.ObtenerUltimosAsync("305", 2);

        Assert.Equal(["m4", "m5"], turnos.Select(t => t.Contenido));
    }

    [Fact]
    public async Task Las_conversaciones_de_dos_huespedes_no_se_mezclan()
    {
        var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorEnMemoria());
        await servicio.AgregarTurnoAsync(Turno("305", "secreto del 305"));
        await servicio.AgregarTurnoAsync(Turno("412", "hola desde el 412"));

        Assert.Equal(["secreto del 305"], (await servicio.ObtenerUltimosAsync("305", 10)).Select(t => t.Contenido));
        Assert.Equal(["hola desde el 412"], (await servicio.ObtenerUltimosAsync("412", 10)).Select(t => t.Contenido));
        Assert.Empty(await servicio.ObtenerUltimosAsync("999", 10));
    }

    [Fact]
    public async Task El_checkout_borra_la_conversacion_y_las_preferencias_solo_de_ese_huesped()
    {
        var almacen = new AlmacenMemoriaClaveValorEnMemoria();
        var conversacion = new ServicioConversacion(almacen);
        var preferencias = new ServicioPreferencias(almacen);
        await conversacion.AgregarTurnoAsync(Turno("305", "hola"));
        await preferencias.GuardarAsync("305", NombresPreferencia.Dieta, "vegano");
        await conversacion.AgregarTurnoAsync(Turno("412", "hola"));

        await almacen.EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped("305"));

        Assert.Empty(await conversacion.ObtenerUltimosAsync("305", 10));
        Assert.Null(await preferencias.ObtenerAsync("305", NombresPreferencia.Dieta));
        Assert.Single(await conversacion.ObtenerUltimosAsync("412", 10));
    }

    [Fact]
    public async Task Rechaza_ids_de_huesped_con_dos_puntos()
    {
        var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorEnMemoria());

        await Assert.ThrowsAnyAsync<ArgumentException>(() => servicio.AgregarTurnoAsync(Turno("30:5", "x")));
    }

    [Fact]
    public async Task Pedir_cero_turnos_es_un_error()
    {
        var servicio = new ServicioConversacion(new AlmacenMemoriaClaveValorEnMemoria());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => servicio.ObtenerUltimosAsync("305", 0));
    }
}
