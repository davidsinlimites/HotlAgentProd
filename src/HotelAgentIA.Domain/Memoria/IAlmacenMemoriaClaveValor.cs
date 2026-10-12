namespace HotelAgentIA.Domain.Memoria;
public interface IAlmacenMemoriaClaveValor
{
    Task<string?> ObtenerAsync(string clave, CancellationToken ct = default);
    Task GuardarAsync(string clave, string valor, CancellationToken ct = default);

    /// <summary>
    /// Lectura-modificacion-escritura atomica de una clave: nadie mas la cambia entre la lectura y la
    /// escritura. Recibe el valor actual (null si no existe) y devuelve el nuevo. La funcion debe ser
    /// rapida y sin efectos secundarios: puede mantener un bloqueo mientras se ejecuta.
    /// </summary>
    Task ActualizarAsync(string clave, Func<string?, string> actualizar, CancellationToken ct = default);
    Task EliminarAsync(string clave, CancellationToken ct = default);
    Task EliminarPorPrefijoAsync(string prefijo, CancellationToken ct = default); // checkout (fase 31)
}
