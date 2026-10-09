using System.Collections.Concurrent;
using HotelAgentIA.Domain.Memoria;

namespace HotelAgentIA.Infrastructure.Memoria;

public sealed class AlmacenMemoriaClaveValorEnMemoria : IAlmacenMemoriaClaveValor
{
    private readonly ConcurrentDictionary<string, string> _datos = new();

    public Task<string?> ObtenerAsync(string clave, CancellationToken ct = default)
    {
        _datos.TryGetValue(clave, out var valor);
        return Task.FromResult(valor);
    }
    public Task GuardarAsync(string clave, string valor, CancellationToken ct = default)
    {
        _datos[clave] = valor;
        return Task.CompletedTask;
    }
    public Task EliminarAsync(string clave, CancellationToken ct = default) 
    { 
        _datos.TryRemove(clave, out _);
        return Task.CompletedTask;
    }
    //Analizar este método porque es importante
    public Task EliminarPorPrefijoAsync(string prefijo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefijo);

        var clavesAEliminar = _datos.Keys
            .Where(k => k.StartsWith(prefijo, StringComparison.Ordinal))
            .ToList();

        foreach (var clave in clavesAEliminar)
        {
            _datos.TryRemove(clave, out _);
        }
        return Task.CompletedTask;
    }

    // Solo para el banco de pruebas: no está en la interfaz
    public IReadOnlyCollection<string> Claves => _datos.Keys.ToList();
}