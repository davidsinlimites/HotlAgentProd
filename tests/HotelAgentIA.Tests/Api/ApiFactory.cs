using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelAgentIA.Application.Hotel;
using HotelAgentIA.Domain.Hotel;
using HotelAgentIA.Domain.Memoria;
using HotelAgentIA.Infrastructure.Chat;
using HotelAgentIA.Infrastructure.Memoria;
using HotelAgentIA.Tests.Hotel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotelAgentIA.Tests.Api;

/// <summary>
/// La API real (JWT, CORS, limite de uso, endpoints) en memoria, pero sin base de datos ni modelo:
/// repositorio, almacen de memoria y cliente de chat son falsos, asi que no hay red ni coste.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string OrigenPermitido = "https://web.prueba";

    // Valores de prueba: Program carga el .env real de la raiz, pero las variables de entorno ya
    // definidas ganan (NoClobber). Se fijan TODAS las que usa la API para que ninguna prueba
    // llegue por error a la base ni al modelo reales.
    static ApiFactory()
    {
        Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", "clave-solo-de-pruebas-0123456789-abcdefghijklmnopqrstuvwxyz");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "HotelAgentIA");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "hotel-huespedes");
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION", "Host=127.0.0.1;Port=1;Database=no-existe;Username=x;Password=x;Timeout=1");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://no-existe.invalid");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "x");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", "x");
        Environment.SetEnvironmentVariable("DEV_TOKENS", "true");
        Environment.SetEnvironmentVariable("CORS_ORIGINS", OrigenPermitido);
    }

    private readonly Func<IChatClient> _modelo;

    /// <summary>Por defecto el modelo llama a consultar_mi_estancia y devuelve su resultado.</summary>
    public ApiFactory()
        : this(() => ClienteChatFalso.ConLlamadaAHerramienta("consultar_mi_estancia"))
    {
    }

    internal ApiFactory(Func<IChatClient> modelo)
    {
        _modelo = modelo;
    }

    public AlmacenMemoriaClaveValorEnMemoria Almacen { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(servicios =>
        {
            // El reloj sigue siendo el real (el JWT se valida contra el reloj real); las estancias se calculan respecto a ahora
            servicios.RemoveAll<IRepositorioInfoHotel>();
            servicios.AddScoped<IRepositorioInfoHotel>(_ => new RepositorioFalso(DateTimeOffset.UtcNow));

            servicios.RemoveAll<IAlmacenMemoriaClaveValor>();
            servicios.AddSingleton<IAlmacenMemoriaClaveValor>(Almacen);

            servicios.RemoveAll<IChatClient>();
            servicios.AddSingleton(_ => _modelo());
        });
    }

    public async Task<string> ObtenerTokenAsync(HttpClient cliente, string huespedId)
    {
        var respuesta = await cliente.PostAsJsonAsync("/dev/token", new { huespedId });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return cuerpo.GetProperty("token").GetString()!;
    }

    public static HttpRequestMessage Peticion(string ruta, object cuerpo, string? token)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = JsonContent.Create(cuerpo) };
        if (token is not null)
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return peticion;
    }
}

/// <summary>Modelo que falla: sirve para comprobar como la API traduce los errores.</summary>
public sealed class ClienteQueFalla(Exception excepcion, string? textoAntes = null) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromException<ChatResponse>(excepcion);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (textoAntes is not null)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, textoAntes);
            await Task.Yield();
        }

        throw excepcion;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>Respuesta HTTP minima para construir una ClientResultException con un codigo de estado.</summary>
public sealed class RespuestaHttpFalsa(int estado) : PipelineResponse
{
    private sealed class Cabeceras : PipelineResponseHeaders
    {
        public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
        public override bool TryGetValue(string name, out string? value) { value = null; return false; }
        public override bool TryGetValues(string name, out IEnumerable<string>? values) { values = null; return false; }
    }

    public override int Status => estado;
    public override string ReasonPhrase => "";
    public override Stream? ContentStream { get; set; }
    public override BinaryData Content => BinaryData.Empty;
    protected override PipelineResponseHeaders HeadersCore { get; } = new Cabeceras();
    public override BinaryData BufferContent(CancellationToken cancellationToken = default) => BinaryData.Empty;
    public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(BinaryData.Empty);
    public override void Dispose()
    {
    }
}
