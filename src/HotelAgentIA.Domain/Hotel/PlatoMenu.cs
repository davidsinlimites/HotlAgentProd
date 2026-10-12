namespace HotelAgentIA.Domain.Hotel;

/// <summary>Comidas: franjas en que se sirve el plato; vacia = cualquier comida.</summary>
public sealed record PlatoMenu(string Id, string RestauranteId, string Nombre, string Descripcion, decimal Precio, IReadOnlyList<string> EtiquetasDieteticas, IReadOnlyList<Comida> Comidas);
