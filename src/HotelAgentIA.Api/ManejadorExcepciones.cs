using Microsoft.AspNetCore.Diagnostics;

namespace HotelAgentIA.Api;

/// <summary>
/// Convierte en 400 las peticiones mal formadas (JSON invalido, cuerpo ilegible), que de otro modo
/// el middleware de excepciones dejaria como 500. El resto sigue el camino normal (500 sin detalles).
/// </summary>
public sealed class ManejadorExcepciones(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        if (excepcion is not BadHttpRequestException)
        {
            return false;
        }

        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = { Status = StatusCodes.Status400BadRequest, Detail = "La petición no es válida." }
        });
    }
}
