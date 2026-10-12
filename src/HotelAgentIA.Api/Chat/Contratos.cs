using System.ComponentModel.DataAnnotations;

namespace HotelAgentIA.Api.Chat;

public sealed record PeticionChat(
    [property: Required, StringLength(PeticionChat.LongitudMaxima, MinimumLength = 1)] string Mensaje)
{
    public const int LongitudMaxima = 2000;
}

public sealed record RespuestaChat(string Respuesta);

public sealed record PeticionToken([property: Required] string HuespedId);

public sealed record RespuestaToken(string Token, DateTimeOffset Caduca);
