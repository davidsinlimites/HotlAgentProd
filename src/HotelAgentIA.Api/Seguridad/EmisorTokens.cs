using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelAgentIA.Domain.Hotel;
using Microsoft.IdentityModel.Tokens;

namespace HotelAgentIA.Api.Seguridad;

/// <summary>
/// Emite el token del huesped: sub = id del huesped, habitacion, y caducidad que no pasa de la
/// salida de su estancia. El huesped que usa la API sale siempre de este token, nunca del modelo.
/// </summary>
public sealed class EmisorTokens(OpcionesJwt opciones, TimeProvider reloj)
{
    public static readonly TimeSpan DuracionMaxima = TimeSpan.FromHours(12);

    public string Emitir(Estancia estancia)
    {
        var ahora = reloj.GetUtcNow();
        var caduca = ahora + DuracionMaxima;
        if (estancia.Salida < caduca)
        {
            caduca = estancia.Salida;
        }

        var token = new JwtSecurityToken(
            issuer: opciones.Emisor,
            audience: opciones.Audiencia,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, estancia.HuespedId),
                new Claim("habitacion", estancia.NumeroHabitacion)
            ],
            notBefore: ahora.UtcDateTime,
            expires: caduca.UtcDateTime,
            signingCredentials: new SigningCredentials(opciones.Clave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
