namespace HotelAgentIA.Infrastructure.Persistencia.Entidades;

public class EstanciaEntidad
{
    public required string Id { get; set; }
    public required string HuespedId { get; set; }
    public required string NumeroHabitacion { get; set; }

    // Npgsql guarda DateTimeOffset como timestamptz y exige offset 0 (UTC)
    public DateTimeOffset Entrada { get; set; }
    public DateTimeOffset Salida { get; set; }
}
