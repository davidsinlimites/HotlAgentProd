namespace HotelAgentIA.Infrastructure.Persistencia.Entidades;

public class HorarioAperturaEntidad
{
    public int Id { get; set; }
    public required string RestauranteId { get; set; }
    public DayOfWeek Dia { get; set; }
    public TimeOnly Abre { get; set; }
    public TimeOnly Cierra { get; set; }
}
