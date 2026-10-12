using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace HotelAgentIA.Infrastructure.Chat;

/// <summary>
/// IChatClient de prueba para depurar y evaluar sin llamar al modelo: no usa red ni cuesta nada.
/// La respuesta sale de un delegado; por defecto repite el ultimo mensaje del usuario.
/// Tambien puede simular que el modelo llama a una herramienta (ver ConLlamadaAHerramienta).
/// </summary>
public sealed class ClienteChatFalso : IChatClient
{
    private readonly Func<IReadOnlyList<ChatMessage>, ChatOptions?, IList<AIContent>> _generar;
    private readonly TimeSpan _retardoPorPalabra;

    public ClienteChatFalso(Func<IReadOnlyList<ChatMessage>, string>? responder = null, TimeSpan? retardoPorPalabra = null)
        : this(CrearGeneradorDeTexto(responder ?? RepetirUltimoMensajeDelUsuario), retardoPorPalabra ?? TimeSpan.Zero)
    {
    }

    private ClienteChatFalso(Func<IReadOnlyList<ChatMessage>, ChatOptions?, IList<AIContent>> generar, TimeSpan retardoPorPalabra)
    {
        _generar = generar;
        _retardoPorPalabra = retardoPorPalabra;
    }

    /// <summary>
    /// Simula un modelo que, si la herramienta esta disponible en la peticion, la llama con los
    /// argumentos dados y despues responde con el resultado. Si la herramienta no llega en
    /// ChatOptions.Tools, lo dice: sirve para comprobar que las herramientas por peticion llegan.
    /// </summary>
    public static ClienteChatFalso ConLlamadaAHerramienta(string herramienta, IDictionary<string, object?>? argumentos = null) =>
        new((mensajes, opciones) =>
        {
            var resultado = mensajes
                .SelectMany(m => m.Contents)
                .OfType<FunctionResultContent>()
                .LastOrDefault();

            if (resultado is not null)
            {
                return [new TextContent($"[modo offline] Resultado de {herramienta}: {resultado.Result}")];
            }

            var disponible = opciones?.Tools?.OfType<AIFunction>().Any(f => f.Name == herramienta) == true;
            return disponible
                ? [new FunctionCallContent(Guid.NewGuid().ToString("N"), herramienta, argumentos)]
                : [new TextContent($"[modo offline] La herramienta {herramienta} no esta disponible en esta peticion.")];
        }, TimeSpan.Zero);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var contenido = _generar(messages.ToList(), options);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, contenido)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var contenido in _generar(messages.ToList(), options))
        {
            if (contenido is not TextContent texto)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, [contenido]);
                continue;
            }

            var palabras = (texto.Text ?? "").Split(' ');
            for (var i = 0; i < palabras.Length; i++)
            {
                await Task.Delay(_retardoPorPalabra, cancellationToken);
                var fragmento = i == 0 ? palabras[i] : " " + palabras[i];
                yield return new ChatResponseUpdate(ChatRole.Assistant, fragmento);
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    private static Func<IReadOnlyList<ChatMessage>, ChatOptions?, IList<AIContent>> CrearGeneradorDeTexto(
        Func<IReadOnlyList<ChatMessage>, string> responder) =>
        (mensajes, _) => [new TextContent(responder(mensajes))];

    private static string RepetirUltimoMensajeDelUsuario(IReadOnlyList<ChatMessage> mensajes)
    {
        var ultimo = mensajes.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        return string.IsNullOrWhiteSpace(ultimo)
            ? "[modo offline] No recibi ningun mensaje."
            : $"[modo offline] Recibi: {ultimo}";
    }
}
