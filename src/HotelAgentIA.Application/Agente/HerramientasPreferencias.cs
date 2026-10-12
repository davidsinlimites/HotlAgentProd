using System.ComponentModel;
using HotelAgentIA.Application.Memoria;
using Microsoft.Extensions.AI;

namespace HotelAgentIA.Application.Agente;

/// <summary>
/// Unica herramienta de escritura del agente. Se construye por peticion con el huesped del token,
/// asi que solo puede escribir en las preferencias del propio huesped, y solo valores validos de
/// un conjunto cerrado (ValidadorPreferencias).
/// </summary>
public sealed class HerramientasPreferencias
{
    private readonly ServicioPreferencias _preferencias;
    private readonly string _huespedId;

    public HerramientasPreferencias(ServicioPreferencias preferencias, string huespedId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(huespedId);
        _preferencias = preferencias;
        _huespedId = huespedId;
    }

    public IList<AITool> ObtenerHerramientas() =>
    [
        AIFunctionFactory.Create(GuardarPreferencia, "guardar_preferencia")
    ];

    [Description("Guarda una preferencia que el huesped acaba de expresar de forma clara. " +
                 "Preferencias: dieta (ninguna, vegetariano, vegano, sin-gluten, sin-lactosa, halal, kosher), " +
                 "idioma (es, en, fr, de, it, pt), hora-desayuno (HH:mm). Solo si el huesped lo dijo; no la deduzcas.")]
    private async Task<string> GuardarPreferencia(
        [Description("Nombre: dieta, idioma o hora-desayuno.")] string nombre,
        [Description("Valor permitido para esa preferencia.")] string valor,
        CancellationToken ct = default)
    {
        if (!ValidadorPreferencias.TryValidar(nombre, valor, out var normalizado, out var error))
        {
            return error;
        }

        await _preferencias.GuardarAsync(_huespedId, nombre.Trim().ToLowerInvariant(), normalizado, ct);
        return $"Guardado: {nombre.Trim().ToLowerInvariant()} = {normalizado}.";
    }
}
