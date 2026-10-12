using System.Globalization;

namespace HotelAgentIA.Application.Memoria;

/// <summary>
/// Valida lo que el modelo quiere guardar como preferencia: nombre y valor de un conjunto cerrado
/// (o con formato fijo). Lo que dijo el huesped nunca se guarda como texto libre, asi no se puede
/// colar una instruccion por esta via.
/// </summary>
public static class ValidadorPreferencias
{
    private static readonly IReadOnlyDictionary<string, string[]> ValoresCerrados = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [NombresPreferencia.Dieta] = ["ninguna", "vegetariano", "vegano", "sin-gluten", "sin-lactosa", "halal", "kosher"],
        [NombresPreferencia.Idioma] = ["es", "en", "fr", "de", "it", "pt"]
    };

    /// <summary>Devuelve el valor normalizado, o null y el motivo del rechazo.</summary>
    public static bool TryValidar(string? nombre, string? valor, out string valorNormalizado, out string error)
    {
        valorNormalizado = "";
        var n = (nombre ?? "").Trim().ToLowerInvariant();
        var v = (valor ?? "").Trim().ToLowerInvariant();

        if (!NombresPreferencia.Todos.Contains(n, StringComparer.Ordinal))
        {
            error = $"Preferencia '{nombre}' no reconocida. Validas: {string.Join(", ", NombresPreferencia.Todos)}.";
            return false;
        }

        if (n == NombresPreferencia.HoraDesayuno)
        {
            if (!TimeOnly.TryParseExact(v, "HH':'mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            {
                error = "La hora debe tener formato HH:mm, por ejemplo 08:30.";
                return false;
            }

            valorNormalizado = hora.ToString("HH':'mm", CultureInfo.InvariantCulture);
            error = "";
            return true;
        }

        var permitidos = ValoresCerrados[n];
        if (!permitidos.Contains(v, StringComparer.Ordinal))
        {
            error = $"Valor '{valor}' no valido para {n}. Validos: {string.Join(", ", permitidos)}.";
            return false;
        }

        valorNormalizado = v;
        error = "";
        return true;
    }
}
