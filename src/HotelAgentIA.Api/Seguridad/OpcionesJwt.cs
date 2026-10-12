using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HotelAgentIA.Api.Seguridad;

/// <summary>
/// Configuracion del JWT del huesped. La clave de firma viene de JWT_SIGNING_KEY (variable de
/// entorno o .env), nunca del repositorio.
/// </summary>
public sealed record OpcionesJwt(string Emisor, string Audiencia, SymmetricSecurityKey Clave)
{
    public const int LongitudMinimaClave = 32;

    public static OpcionesJwt Cargar(IConfiguration configuracion)
    {
        var clave = configuracion["JWT_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(clave) || Encoding.UTF8.GetByteCount(clave) < LongitudMinimaClave)
        {
            throw new InvalidOperationException(
                $"Falta JWT_SIGNING_KEY o tiene menos de {LongitudMinimaClave} bytes. Defínela en el .env o en las variables de entorno.");
        }

        return new OpcionesJwt(
            configuracion["JWT_ISSUER"] ?? "HotelAgentIA",
            configuracion["JWT_AUDIENCE"] ?? "hotel-huespedes",
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)));
    }
}
