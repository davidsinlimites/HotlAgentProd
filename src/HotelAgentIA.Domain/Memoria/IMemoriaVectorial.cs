namespace HotelAgentIA.Domain.Memoria;

public interface IMemoriaVectorial
{
    Task AgregarAsync(FragmentoMemoria fragmento, CancellationToken ct = default);
    Task<IReadOnlyList<MemoriaPuntuada>> BuscarAsync(
        string huespedId, string consulta, int topK, double puntajeMinimo, CancellationToken ct = default);
}
