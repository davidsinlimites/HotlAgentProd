namespace HotelAgentIA.Infrastructure.Persistencia.Entidades;

/// <summary>Fila del almacen clave-valor de memoria por huesped (claves "huesped:{id}:...").</summary>
public class MemoriaClaveValorEntidad
{
    public required string Clave { get; set; }
    public required string Valor { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }
}
