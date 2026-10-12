using HotelAgentIA.Domain.Memoria;
using HotelAgentIA.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace HotelAgentIA.Infrastructure.Memoria;

/// <summary>
/// Almacen clave-valor durable sobre PostgreSQL (tabla memoria_clave_valor). Sobrevive a reinicios
/// y a los reciclados del pool de IIS. Va por ambito: usa el HotelDbContext de la peticion.
/// </summary>
public sealed class AlmacenMemoriaClaveValorPostgres(HotelDbContext db, TimeProvider reloj) : IAlmacenMemoriaClaveValor
{
    public Task<string?> ObtenerAsync(string clave, CancellationToken ct = default) =>
        db.Memoria.AsNoTracking()
            .Where(m => m.Clave == clave)
            .Select(m => (string?)m.Valor)
            .FirstOrDefaultAsync(ct);

    public Task GuardarAsync(string clave, string valor, CancellationToken ct = default) =>
        GuardarSinTransaccionAsync(clave, valor, ct);

    // El bloqueo consultivo (advisory lock) serializa por clave, incluso si la fila aun no existe, y se
    // libera solo al terminar la transaccion. Vale entre instancias de la API, no solo dentro de un proceso.
    public async Task ActualizarAsync(string clave, Func<string?, string> actualizar, CancellationToken ct = default)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({clave}, 0))", ct);

        var nuevo = actualizar(await ObtenerAsync(clave, ct));
        await GuardarSinTransaccionAsync(clave, nuevo, ct);

        await transaccion.CommitAsync(ct);
    }

    public Task EliminarAsync(string clave, CancellationToken ct = default) =>
        db.Memoria.Where(m => m.Clave == clave).ExecuteDeleteAsync(ct);

    // StartsWith se traduce a LIKE escapando % y _, y distingue mayusculas (comparacion ordinal)
    public Task EliminarPorPrefijoAsync(string prefijo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefijo);
        return db.Memoria.Where(m => m.Clave.StartsWith(prefijo)).ExecuteDeleteAsync(ct);
    }

    private Task GuardarSinTransaccionAsync(string clave, string valor, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        return db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO memoria_clave_valor ("Clave", "Valor", "ActualizadoEn")
             VALUES ({clave}, {valor}, {ahora})
             ON CONFLICT ("Clave") DO UPDATE SET "Valor" = EXCLUDED."Valor", "ActualizadoEn" = EXCLUDED."ActualizadoEn"
             """, ct);
    }
}
