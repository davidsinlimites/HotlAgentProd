namespace HotelAgentIA.Domain.Hotel;

public sealed record PlatoMenu(string Id, string RestauranteId, string Nombre, string Descripcion, decimal Precio, IReadOnlyList<string> EtiquetasDieteticas);
