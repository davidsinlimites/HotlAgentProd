using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace HotelAgentIA.Infrastructure.Chat;

/// <summary>
/// Crea el IChatClient del modelo en Azure OpenAI/Foundry. El endpoint de Foundry es el del
/// proyecto; se usa el cliente OpenAI apuntando a &lt;host&gt;/openai/v1/ con la clave.
/// </summary>
public static class FabricaClienteChat
{
    public static IChatClient Crear(string endpoint, string clave, string despliegue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);
        ArgumentException.ThrowIfNullOrWhiteSpace(despliegue);

        var url = new Uri(new Uri(endpoint).GetLeftPart(UriPartial.Authority) + "/openai/v1/");
        return new OpenAIClient(new ApiKeyCredential(clave), new OpenAIClientOptions { Endpoint = url })
            .GetChatClient(despliegue)
            .AsIChatClient();
    }

    /// <summary>Lee AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY y AZURE_OPENAI_DEPLOYMENT; null si falta alguna.</summary>
    public static IChatClient? CrearDesdeEntorno()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var clave = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var despliegue = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

        return string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(despliegue)
            ? null
            : Crear(endpoint, clave, despliegue);
    }
}
