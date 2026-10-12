using System.ClientModel;
using System.Text.Json;
using HotelAgentIA.Api.Seguridad;
using HotelAgentIA.Application.Agente;

namespace HotelAgentIA.Api.Chat;

public static class PuntosFinalesChat
{
    public const string PoliticaLimite = "chat";

    private const string MensajeFiltro = "No puedo ayudar con ese mensaje. ¿Puedes reformularlo?";
    private const string MensajeFallo = "No pude responder ahora mismo. Inténtalo de nuevo en un momento o consulta en recepción.";

    public static void MapearChat(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/chat").RequireAuthorization().RequireRateLimiting(PoliticaLimite);

        grupo.MapPost("/", ResponderAsync).WithName("Chat").WithSummary("Responde al mensaje del huesped autenticado.");
        grupo.MapPost("/stream", ResponderEnStreamingAsync).WithName("ChatStream")
            .WithSummary("Igual que /chat pero por SSE: eventos 'data' con trozos de texto en JSON y un evento 'fin'.");
    }

    private static async Task<IResult> ResponderAsync(
        PeticionChat peticion, HttpContext contexto, AsistenteHotel asistente, ILogger<AsistenteHotel> log, CancellationToken ct)
    {
        if (!Validar(peticion, out var error))
        {
            return error!;
        }

        var huespedId = contexto.User.HuespedId();
        try
        {
            return Results.Ok(new RespuestaChat(await asistente.ResponderAsync(huespedId, peticion.Mensaje, ct)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Traducir(ex, log, out _);
        }
    }

    private static async Task ResponderEnStreamingAsync(
        PeticionChat peticion, HttpContext contexto, AsistenteHotel asistente, ILogger<AsistenteHotel> log, CancellationToken ct)
    {
        if (!Validar(peticion, out var error))
        {
            await error!.ExecuteAsync(contexto);
            return;
        }

        var huespedId = contexto.User.HuespedId();
        var respuesta = contexto.Response;
        var empezado = false;

        try
        {
            await foreach (var trozo in asistente.ResponderEnStreamingAsync(huespedId, peticion.Mensaje, ct))
            {
                if (!empezado)
                {
                    PrepararSse(contexto);
                    empezado = true;
                }

                await respuesta.WriteAsync($"data: {JsonSerializer.Serialize(trozo)}\n\n", ct);
                await respuesta.Body.FlushAsync(ct);
            }

            if (!empezado)
            {
                PrepararSse(contexto);
            }

            await respuesta.WriteAsync("event: fin\ndata: {}\n\n", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var resultado = Traducir(ex, log, out var mensaje);
            if (!empezado)
            {
                await resultado.ExecuteAsync(contexto);
                return;
            }

            // Las cabeceras ya salieron: el error viaja como evento
            await respuesta.WriteAsync($"event: error\ndata: {JsonSerializer.Serialize(mensaje)}\n\n", CancellationToken.None);
        }
    }

    private static void PrepararSse(HttpContext contexto)
    {
        contexto.Response.ContentType = "text/event-stream";
        contexto.Response.Headers.CacheControl = "no-cache";
        contexto.Response.Headers["X-Accel-Buffering"] = "no";
        contexto.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();
    }

    private static bool Validar(PeticionChat? peticion, out IResult? error)
    {
        if (peticion is null || string.IsNullOrWhiteSpace(peticion.Mensaje) || peticion.Mensaje.Length > PeticionChat.LongitudMaxima)
        {
            error = Results.Problem(
                $"El mensaje es obligatorio y admite hasta {PeticionChat.LongitudMaxima} caracteres.", statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        error = null;
        return true;
    }

    // Se registra solo el tipo de error, nunca el mensaje del huesped ni la respuesta
    private static IResult Traducir(Exception ex, ILogger log, out string mensaje)
    {
        if (ex is ClientResultException { Status: 400 } filtro && filtro.Message.Contains("content_filter", StringComparison.Ordinal))
        {
            log.LogWarning("Mensaje bloqueado por el filtro de contenido del modelo");
            mensaje = MensajeFiltro;
            return Results.Problem(MensajeFiltro, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        log.LogError("Fallo al responder al huesped: {Tipo}", ex.GetType().Name);
        mensaje = MensajeFallo;
        return Results.Problem(MensajeFallo, statusCode: StatusCodes.Status502BadGateway);
    }
}
