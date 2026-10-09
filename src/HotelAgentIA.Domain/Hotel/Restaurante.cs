namespace HotelAgentIA.Domain.Hotel;

public sealed record Restaurante(string Id, string Nombre, string Descripcion, string Ubicacion, IReadOnlyList<HorarioApertura> Horarios);
