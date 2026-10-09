namespace HotelAgentIA.Domain.Memoria;
public interface IAlmacenMemoriaClaveValor
{
    Task<string?> ObtenerAsync(string clave, CancellationToken ct = default);
    Task GuardarAsync(string clave, string valor, CancellationToken ct = default);
    Task EliminarAsync(string clave, CancellationToken ct = default);
    Task EliminarPorPrefijoAsync(string prefijo, CancellationToken ct = default); // checkout (fase 31)
}
