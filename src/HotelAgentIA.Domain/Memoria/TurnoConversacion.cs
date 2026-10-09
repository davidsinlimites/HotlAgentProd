namespace HotelAgentIA.Domain.Memoria;

public sealed record TurnoConversacion(string HuespedId, RolConversacion Rol, string Contenido, DateTimeOffset Fecha);
