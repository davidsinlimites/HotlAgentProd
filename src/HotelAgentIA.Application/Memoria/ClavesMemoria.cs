namespace HotelAgentIA.Application.Memoria;

/// <summary>
/// Único lugar donde se construyen las claves del almacén clave-valor.
/// Todas las claves de un huésped empiezan por PrefijoHuesped, así que
/// EliminarPorPrefijoAsync(PrefijoHuesped(id)) borra todo lo suyo y nada más.
/// </summary>
public static class ClavesMemoria
{
    // "huesped:305:"  (los dos puntos finales evitan que "huesped:30:" sea prefijo de "huesped:305:")
    public static string PrefijoHuesped(string huespedId)
    {
        ValidarSegmento(huespedId, nameof(huespedId));
        return $"huesped:{huespedId}:";
    }

    // "huesped:305:preferencia:dieta"
    public static string Preferencia(string huespedId, string nombre)
    {
        ValidarSegmento(nombre, nameof(nombre));
        return $"{PrefijoHuesped(huespedId)}preferencia:{nombre}";
    }

    // "huesped:305:conversacion"
    public static string Conversacion(string huespedId)
    {
        return $"{PrefijoHuesped(huespedId)}conversacion";
    }

    // Un segmento no puede estar vacío ni contener ':' porque ':' es el separador de la clave.
    private static void ValidarSegmento(string valor, string nombreParametro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, nombreParametro);

        if (valor.Contains(':'))
        {
            throw new ArgumentException("No puede contener ':'.", nombreParametro);
        }
    }
}