using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

// Prueba de concepto de la fase A: comprobar en el hosting que el streaming SSE,
// la memoria y la salida al modelo funcionan. Se sustituirá por la API real (fase C).

// En local las claves vienen de un .env (ignorado por git) buscado hacia arriba desde la carpeta actual;
// en el hosting vienen de variables de entorno, que tienen prioridad porque Env no las pisa.
DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    estado = "ok",
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    memoriaMb = Environment.WorkingSet / 1024 / 1024,
    horaUtc = DateTimeOffset.UtcNow
}));

// Emite un evento por segundo. Si en el hosting llegan todos de golpe al final,
// hay buffering y SSE no sirve tal cual.
app.MapGet("/stream", async (HttpContext contexto, int? eventos, CancellationToken cancelacion) =>
{
    contexto.Response.ContentType = "text/event-stream";
    contexto.Response.Headers.CacheControl = "no-cache";
    contexto.Response.Headers["X-Accel-Buffering"] = "no";
    contexto.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

    var total = Math.Clamp(eventos ?? 10, 1, 60);
    for (var i = 1; i <= total; i++)
    {
        await contexto.Response.WriteAsync($"data: evento {i}/{total} a las {DateTimeOffset.UtcNow:HH:mm:ss}\n\n", cancelacion);
        await contexto.Response.Body.FlushAsync(cancelacion);
        await Task.Delay(TimeSpan.FromSeconds(1), cancelacion);
    }
});

// Llama al modelo de Azure OpenAI con clave de API (en el hosting no hay identidad administrada).
// Variables: AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT.
app.MapGet("/modelo", async (string? q, CancellationToken cancelacion) =>
{
    var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    var clave = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
    var despliegue = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(despliegue))
    {
        return Results.Problem(
            "Faltan AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY o AZURE_OPENAI_DEPLOYMENT.", statusCode: 503);
    }

    AIAgent agente = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(clave))
        .GetChatClient(despliegue)
        .AsAIAgent(instructions: "Eres un asistente breve. Responde en una frase.");

    var respuesta = await agente.RunAsync(q ?? "Di hola en español.", cancellationToken: cancelacion);
    return Results.Ok(new { respuesta = respuesta.ToString() });
});

app.Run();
