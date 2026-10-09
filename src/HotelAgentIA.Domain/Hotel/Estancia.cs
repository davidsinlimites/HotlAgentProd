namespace HotelAgentIA.Domain.Hotel;

public sealed record Estancia(string Id, string HuespedId, string NumeroHabitacion,
    DateTimeOffset Entrada, DateTimeOffset Salida)
{
    public bool EstaActivaEn(DateTimeOffset ahora) => ahora >= Entrada && ahora < Salida;
}
