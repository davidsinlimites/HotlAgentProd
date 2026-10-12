namespace HotelAgentIA.Infrastructure.Persistencia.Entidades;

public class RestauranteEntidad
{
    public required string Id { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public required string Ubicacion { get; set; }

    public List<HorarioAperturaEntidad> Horarios { get; set; } = [];
    public List<PlatoMenuEntidad> Platos { get; set; } = [];
}
