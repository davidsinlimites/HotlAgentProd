namespace HotelAgentIA.Infrastructure.Persistencia.Entidades;

public class PlatoMenuEntidad
{
    public required string Id { get; set; }
    public required string RestauranteId { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string[] EtiquetasDieteticas { get; set; } = [];

    /// <summary>Franjas en minusculas (desayuno, almuerzo, cena); vacia = cualquier comida.</summary>
    public string[] Comidas { get; set; } = [];
}
