using System.Security.Claims;

namespace HotelAgentIA.Api.Seguridad;

public static class UsuarioExtensiones
{
    /// <summary>Id del huesped del token (claim sub). Los endpoints protegidos garantizan que existe.</summary>
    public static string HuespedId(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue("sub") ?? throw new InvalidOperationException("El token no trae el claim sub.");
}
