namespace HotelAgentIA.Infrastructure.Persistencia.Entidades;

public class ActividadEntidad
{
    public required string Id { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public required string Ubicacion { get; set; }
    public DayOfWeek Dia { get; set; }
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
}
