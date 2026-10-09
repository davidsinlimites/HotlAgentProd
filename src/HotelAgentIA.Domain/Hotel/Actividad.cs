namespace HotelAgentIA.Domain.Hotel;

public sealed record Actividad(string Id, string Nombre, string Descripcion, string Ubicacion, DayOfWeek Dia, TimeOnly Inicio, TimeOnly Fin);
