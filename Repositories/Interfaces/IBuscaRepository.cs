using BuscaML.Api.Models;

namespace BuscaML.Api.Repositories.Interfaces;

public interface IBuscaRepository
{
    Task<Busca?> BuscarUltimaPorTermoAsync(string termoNormalizado, CancellationToken ct = default);
    Task<List<ResultadoBusca>> ListarResultadosAsync(int buscaId, CancellationToken ct = default);
    Task AdicionarAsync(Busca busca, CancellationToken ct = default);
    Task AdicionarResultadosAsync(List<ResultadoBusca> resultados, CancellationToken ct = default);
}
