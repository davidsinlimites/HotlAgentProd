using System.ClientModel;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HotelAgentIA.Api.Seguridad;
using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Domain.Hotel;
using HotelAgentIA.Infrastructure.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace HotelAgentIA.Tests.Api;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public ApiTests(ApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private async Task<(HttpStatusCode Estado, string Cuerpo)> ChatAsync(string? token, object cuerpo, string ruta = "/chat")
    {
        using var peticion = ApiFactory.Peticion(ruta, cuerpo, token);
        using var respuesta = await _cliente.SendAsync(peticion);
        return (respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync());
    }

    // --- Salud y contrato ---

    [Fact]
    public async Task Health_es_publico()
    {
        var respuesta = await _cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"estado\":\"ok\"", await respuesta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Los_endpoints_de_prueba_de_la_fase_A_ya_no_existen()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/modelo?q=hola")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/stream")).StatusCode);
    }

    [Fact]
    public async Task OpenAPI_describe_los_endpoints_de_chat()
    {
        var documento = await _cliente.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var rutas = documento.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/chat", rutas);
        Assert.Contains("/chat/stream", rutas);
    }

    // --- Autenticacion ---

    [Fact]
    public async Task Sin_token_el_chat_da_401()
    {
        var (estado, _) = await ChatAsync(null, new { mensaje = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, estado);
    }

    [Fact]
    public async Task Con_un_token_inventado_el_chat_da_401()
    {
        var (estado, _) = await ChatAsync("abc.def.ghi", new { mensaje = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, estado);
    }

    [Fact]
    public async Task Un_token_firmado_con_otra_clave_da_401()
    {
        var ajeno = new OpcionesJwt("HotelAgentIA", "hotel-huespedes",
            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes("otra-clave-distinta-0123456789-abcdefghijklmnopqrstuvwxyz")));
        var token = new EmisorTokens(ajeno, TimeProvider.System).Emitir(EstanciaActiva("305"));

        var (estado, _) = await ChatAsync(token, new { mensaje = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, estado);
    }

    [Fact]
    public async Task Un_token_con_otra_audiencia_da_401()
    {
        var propias = _fabrica.Services.GetRequiredService<OpcionesJwt>();
        var otraAudiencia = propias with { Audiencia = "otra-aplicacion" };
        var token = new EmisorTokens(otraAudiencia, TimeProvider.System).Emitir(EstanciaActiva("305"));

        var (estado, _) = await ChatAsync(token, new { mensaje = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, estado);
    }

    [Fact]
    public async Task Un_token_caducado_da_401()
    {
        var propias = _fabrica.Services.GetRequiredService<OpcionesJwt>();
        var haceDosDias = new RelojDeLaPrueba(DateTimeOffset.UtcNow.AddDays(-2));
        var token = new EmisorTokens(propias, haceDosDias).Emitir(EstanciaActiva("305"));

        var (estado, _) = await ChatAsync(token, new { mensaje = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, estado);
    }

    [Fact]
    public void El_token_no_dura_mas_que_la_estancia()
    {
        var propias = _fabrica.Services.GetRequiredService<OpcionesJwt>();
        var ahora = DateTimeOffset.UtcNow;
        var estancia = new Estancia("e", "305", "305", ahora.AddDays(-1), ahora.AddHours(2));

        var token = new EmisorTokens(propias, TimeProvider.System).Emitir(estancia);

        var caduca = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;
        Assert.InRange(caduca, ahora.UtcDateTime.AddHours(1), ahora.UtcDateTime.AddHours(2).AddMinutes(1));
    }

    [Fact]
    public async Task El_token_lleva_el_huesped_y_la_habitacion()
    {
        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("305", jwt.Subject);
        Assert.Equal("305", jwt.Claims.Single(c => c.Type == "habitacion").Value);
    }

    // --- /dev/token ---

    [Fact]
    public async Task Dev_token_para_un_huesped_sin_estancia_activa_da_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.PostAsJsonAsync("/dev/token", new { huespedId = "999" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.PostAsJsonAsync("/dev/token", new { huespedId = "501" })).StatusCode);
    }

    // --- Aislamiento entre huespedes ---

    [Fact]
    public async Task Cada_huesped_ve_su_propia_estancia()
    {
        var t305 = await _fabrica.ObtenerTokenAsync(_cliente, "305");
        var t412 = await _fabrica.ObtenerTokenAsync(_cliente, "412");

        var (e1, c1) = await ChatAsync(t305, new { mensaje = "¿En qué habitación estoy?" });
        var (e2, c2) = await ChatAsync(t412, new { mensaje = "¿En qué habitación estoy?" });

        Assert.Equal(HttpStatusCode.OK, e1);
        Assert.Equal(HttpStatusCode.OK, e2);
        Assert.Contains("Habitacion 305", c1, StringComparison.Ordinal);
        Assert.Contains("Habitacion 412", c2, StringComparison.Ordinal);
        Assert.DoesNotContain("412", c1, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_huesped_no_puede_elegir_otro_por_el_cuerpo_de_la_peticion()
    {
        var t305 = await _fabrica.ObtenerTokenAsync(_cliente, "305");

        var (estado, cuerpo) = await ChatAsync(t305, new { mensaje = "hola", huespedId = "412", sub = "412" });

        Assert.Equal(HttpStatusCode.OK, estado);
        Assert.Contains("Habitacion 305", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task La_conversacion_queda_guardada_solo_para_el_huesped_del_token()
    {
        // Fabrica propia: el almacen del fixture compartido lo usan otras pruebas con el huesped 412
        using var fabrica = new ApiFactory();
        using var cliente = fabrica.CreateClient();
        var t305 = await fabrica.ObtenerTokenAsync(cliente, "305");
        using var peticion = ApiFactory.Peticion("/chat", new { mensaje = "mensaje propio del 305" }, t305);
        using var respuesta = await cliente.SendAsync(peticion);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var servicio = new ServicioConversacion(fabrica.Almacen);
        Assert.Contains(await servicio.ObtenerUltimosAsync("305", 20), t => t.Contenido == "mensaje propio del 305");
        Assert.Empty(await servicio.ObtenerUltimosAsync("412", 20));
        Assert.All(fabrica.Almacen.Claves, c => Assert.StartsWith("huesped:305:", c, StringComparison.Ordinal));
    }

    // --- Validacion de la peticion ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_mensaje_vacio_da_400(string mensaje)
    {
        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");

        var (estado, _) = await ChatAsync(token, new { mensaje });

        Assert.Equal(HttpStatusCode.BadRequest, estado);
    }

    [Fact]
    public async Task Un_mensaje_de_mas_de_2000_caracteres_da_400_y_uno_de_2000_pasa()
    {
        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");

        Assert.Equal(HttpStatusCode.BadRequest, (await ChatAsync(token, new { mensaje = new string('a', 2001) })).Estado);
        Assert.Equal(HttpStatusCode.OK, (await ChatAsync(token, new { mensaje = new string('a', 2000) })).Estado);
    }

    [Fact]
    public async Task Un_json_roto_da_400_y_no_500()
    {
        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/chat") { Content = new StringContent("{\"mensaje\":", Encoding.UTF8, "application/json") };
        peticion.Headers.Authorization = new("Bearer", token);

        using var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_cuerpo_sin_el_campo_mensaje_da_400()
    {
        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");

        var (estado, _) = await ChatAsync(token, new { otro = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, estado);
    }

    // --- Streaming ---

    [Fact]
    public async Task El_streaming_emite_eventos_data_y_termina_con_fin()
    {
        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");
        using var peticion = ApiFactory.Peticion("/chat/stream", new { mensaje = "mi estancia" }, token);

        using var respuesta = await _cliente.SendAsync(peticion);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/event-stream", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.EndsWith("event: fin\ndata: {}\n\n", cuerpo, StringComparison.Ordinal);

        var texto = string.Concat(cuerpo.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(e => e.StartsWith("data: \"", StringComparison.Ordinal))
            .Select(e => JsonSerializer.Deserialize<string>(e["data: ".Length..])));
        Assert.Contains("Habitacion 305", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_streaming_tambien_exige_token_y_valida_el_mensaje()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await ChatAsync(null, new { mensaje = "hola" }, "/chat/stream")).Estado);

        var token = await _fabrica.ObtenerTokenAsync(_cliente, "305");
        Assert.Equal(HttpStatusCode.BadRequest, (await ChatAsync(token, new { mensaje = "" }, "/chat/stream")).Estado);
    }

    // --- CORS ---

    [Fact]
    public async Task CORS_permite_el_origen_configurado_y_no_otros()
    {
        async Task<string?> OrigenPermitidoParaAsync(string origen)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Options, "/chat");
            peticion.Headers.Add("Origin", origen);
            peticion.Headers.Add("Access-Control-Request-Method", "POST");
            peticion.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            using var respuesta = await _cliente.SendAsync(peticion);
            return respuesta.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores) ? valores.Single() : null;
        }

        Assert.Equal(ApiFactory.OrigenPermitido, await OrigenPermitidoParaAsync(ApiFactory.OrigenPermitido));
        Assert.Null(await OrigenPermitidoParaAsync("https://sitio-malicioso.example"));
    }

    private static Estancia EstanciaActiva(string huesped)
    {
        var ahora = DateTimeOffset.UtcNow;
        return new Estancia($"e-{huesped}", huesped, huesped, ahora.AddDays(-1), ahora.AddDays(3));
    }
}

public class ApiErroresTests
{
    private static async Task<(HttpStatusCode Estado, string Cuerpo)> ChatAsync(ApiFactory fabrica, string ruta = "/chat")
    {
        using var cliente = fabrica.CreateClient();
        var token = await fabrica.ObtenerTokenAsync(cliente, "305");
        using var peticion = ApiFactory.Peticion(ruta, new { mensaje = "hola" }, token);
        using var respuesta = await cliente.SendAsync(peticion);
        return (respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_filtro_de_contenido_del_modelo_da_422_con_mensaje_amable()
    {
        var filtro = new ClientResultException("HTTP 400 (: content_filter)", new RespuestaHttpFalsa(400));
        using var fabrica = new ApiFactory(() => new ClienteQueFalla(filtro));

        var (estado, cuerpo) = await ChatAsync(fabrica);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, estado);
        Assert.Contains("reformularlo", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("content_filter", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Otro_fallo_del_modelo_da_502_sin_filtrar_detalles_internos()
    {
        using var fabrica = new ApiFactory(() => new ClienteQueFalla(new InvalidOperationException("clave-secreta-del-proveedor")));

        var (estado, cuerpo) = await ChatAsync(fabrica);

        Assert.Equal(HttpStatusCode.BadGateway, estado);
        Assert.DoesNotContain("clave-secreta-del-proveedor", cuerpo, StringComparison.Ordinal);
        Assert.Contains("recepción", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_error_400_del_modelo_que_no_es_el_filtro_da_502()
    {
        var otro = new ClientResultException("HTTP 400 (: invalid_request)", new RespuestaHttpFalsa(400));
        using var fabrica = new ApiFactory(() => new ClienteQueFalla(otro));

        var (estado, _) = await ChatAsync(fabrica);

        Assert.Equal(HttpStatusCode.BadGateway, estado);
    }

    [Fact]
    public async Task En_streaming_un_fallo_antes_de_empezar_da_el_codigo_de_error()
    {
        using var fabrica = new ApiFactory(() => new ClienteQueFalla(new InvalidOperationException("x")));

        var (estado, _) = await ChatAsync(fabrica, "/chat/stream");

        Assert.Equal(HttpStatusCode.BadGateway, estado);
    }

    [Fact]
    public async Task En_streaming_un_fallo_a_mitad_viaja_como_evento_error()
    {
        using var fabrica = new ApiFactory(() => new ClienteQueFalla(new InvalidOperationException("secreto"), textoAntes: "Hola"));

        var (estado, cuerpo) = await ChatAsync(fabrica, "/chat/stream");

        Assert.Equal(HttpStatusCode.OK, estado);
        Assert.Contains("data: \"Hola\"", cuerpo, StringComparison.Ordinal);
        Assert.Contains("event: error", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("secreto", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("event: fin", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_limite_de_uso_da_429_a_partir_de_la_peticion_21_y_es_por_huesped()
    {
        using var fabrica = new ApiFactory();
        using var cliente = fabrica.CreateClient();
        var t305 = await fabrica.ObtenerTokenAsync(cliente, "305");
        var t412 = await fabrica.ObtenerTokenAsync(cliente, "412");

        var estados = new List<HttpStatusCode>();
        for (var i = 0; i < 22; i++)
        {
            // Mensaje vacio: la peticion cuenta para el limite pero no llega al modelo
            using var peticion = ApiFactory.Peticion("/chat", new { mensaje = "" }, t305);
            using var respuesta = await cliente.SendAsync(peticion);
            estados.Add(respuesta.StatusCode);
        }

        Assert.All(estados.Take(20), e => Assert.Equal(HttpStatusCode.BadRequest, e));
        Assert.All(estados.Skip(20), e => Assert.Equal(HttpStatusCode.TooManyRequests, e));

        using var otro = ApiFactory.Peticion("/chat", new { mensaje = "hola" }, t412);
        using var respuestaOtro = await cliente.SendAsync(otro);
        Assert.Equal(HttpStatusCode.OK, respuestaOtro.StatusCode);
    }
}

public sealed class RelojDeLaPrueba(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
