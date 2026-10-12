namespace HotelAgentIA.Application.Memoria;

/// <summary>Nombres de las preferencias que el agente conoce. Son segmentos de clave: sin ':' y en minusculas.</summary>
public static class NombresPreferencia
{
    public const string Dieta = "dieta";
    public const string Idioma = "idioma";
    public const string HoraDesayuno = "hora-desayuno";

    public static readonly IReadOnlyList<string> Todos = [Dieta, Idioma, HoraDesayuno];
}
