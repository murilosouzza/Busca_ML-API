using BuscaML.Api.Models;

namespace BuscaML.Api.Repositories.Interfaces;

public interface IHighlightCacheRepository
{
    Task<HighlightCache?> BuscarPorCategoriaAsync(string categoriaId, CancellationToken ct = default);
    Task AdicionarAsync(HighlightCache cache, CancellationToken ct = default);
    Task AtualizarAsync(HighlightCache cache, CancellationToken ct = default);
}
