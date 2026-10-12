using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace HotelAgentIA.Infrastructure.Chat;

/// <summary>
/// IChatClient de prueba para depurar y evaluar sin llamar al modelo: no usa red ni cuesta nada.
/// La respuesta sale de un delegado; por defecto repite el ultimo mensaje del usuario.
/// </summary>
public sealed class ClienteChatFalso : IChatClient
{
    private readonly Func<IReadOnlyList<ChatMessage>, string> _responder;
    private readonly TimeSpan _retardoPorPalabra;

    public ClienteChatFalso(Func<IReadOnlyList<ChatMessage>, string>? responder = null, TimeSpan? retardoPorPalabra = null)
    {
        _responder = responder ?? RepetirUltimoMensajeDelUsuario;
        _retardoPorPalabra = retardoPorPalabra ?? TimeSpan.Zero;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var texto = _responder(messages.ToList());
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, texto)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var palabras = _responder(messages.ToList()).Split(' ');
        for (var i = 0; i < palabras.Length; i++)
        {
            await Task.Delay(_retardoPorPalabra, cancellationToken);
            var fragmento = i == 0 ? palabras[i] : " " + palabras[i];
            yield return new ChatResponseUpdate(ChatRole.Assistant, fragmento);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    private static string RepetirUltimoMensajeDelUsuario(IReadOnlyList<ChatMessage> mensajes)
    {
        var ultimo = mensajes.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        return string.IsNullOrWhiteSpace(ultimo)
            ? "[modo offline] No recibi ningun mensaje."
            : $"[modo offline] Recibi: {ultimo}";
    }
}
