using System.Threading.RateLimiting;
using HotelAgentIA.Api.Chat;
using HotelAgentIA.Api.Seguridad;
using HotelAgentIA.Application.Agente;
using HotelAgentIA.Application.Hotel;
using HotelAgentIA.Application.Memoria;
using HotelAgentIA.Domain.Hotel;
using HotelAgentIA.Domain.Memoria;
using HotelAgentIA.Infrastructure.Chat;
using HotelAgentIA.Infrastructure.Memoria;
using HotelAgentIA.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

// En local las claves vienen de un .env (ignorado por git) buscado hacia arriba desde la carpeta actual;
// en el hosting vienen de variables de entorno, que tienen prioridad porque Env no las pisa.
DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Autenticacion: JWT del huesped. sub = id del huesped; el modelo nunca lo elige ---
var jwt = OpcionesJwt.Cargar(config);
builder.Services.AddSingleton(jwt);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = jwt.Clave,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();

// --- CORS: solo los origenes configurados (CORS_ORIGINS, separados por comas) ---
var origenes = (config["CORS_ORIGINS"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origenes.Length > 0)
    {
        p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
    }
}));

// --- Limite de uso: por huesped (o por IP si no hay token) ---
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(PuntosFinalesChat.PoliticaLimite, contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.User.FindFirst("sub")?.Value ?? contexto.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// --- Datos, memoria y agente ---
var conexion = config["POSTGRES_CONNECTION"] ?? throw new InvalidOperationException("Falta POSTGRES_CONNECTION.");
builder.Services.AddDbContext<HotelDbContext>(o => o.UseNpgsql(conexion));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IRepositorioInfoHotel, RepositorioInfoHotel>();
builder.Services.AddScoped<IServicioInfoHotel, ServicioInfoHotel>();

// Provisional: el almacen en memoria pierde todo al reiniciar el proceso (pendiente: PostgreSQL)
builder.Services.AddSingleton<IAlmacenMemoriaClaveValor, AlmacenMemoriaClaveValorEnMemoria>();
builder.Services.AddSingleton<ServicioPreferencias>();
builder.Services.AddSingleton<ServicioConversacion>();

builder.Services.AddSingleton<IChatClient>(_ => FabricaClienteChat.CrearDesdeEntorno()
    ?? throw new InvalidOperationException("Faltan AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY o AZURE_OPENAI_DEPLOYMENT."));
// Por peticion: usa el DbContext (ambito) a traves de IServicioInfoHotel
builder.Services.AddScoped<AsistenteHotel>();

builder.Services.AddSingleton<EmisorTokens>();
builder.Services.AddExceptionHandler<HotelAgentIA.Api.ManejadorExcepciones>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new
{
    estado = "ok",
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    memoriaMb = Environment.WorkingSet / 1024 / 1024,
    horaUtc = DateTimeOffset.UtcNow
}));

// Emision de tokens para pruebas y la demo: solo con DEV_TOKENS=true y solo para huespedes con estancia activa.
// En produccion lo sustituye el login real del hotel.
if (config.GetValue<bool>("DEV_TOKENS"))
{
    app.MapPost("/dev/token", async (PeticionToken peticion, IServicioInfoHotel info, EmisorTokens emisor, TimeProvider reloj, CancellationToken ct) =>
    {
        var estancia = await info.ObtenerEstanciaAsync(peticion.HuespedId, ct);
        return estancia is null
            ? Results.Problem("El huésped no tiene una estancia activa.", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(new RespuestaToken(emisor.Emitir(estancia), reloj.GetUtcNow() + EmisorTokens.DuracionMaxima));
    }).WithName("TokenDeDesarrollo");
}

app.MapearChat();

app.Run();
