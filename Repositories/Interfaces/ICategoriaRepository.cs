using BuscaML.Api.Models;

namespace BuscaML.Api.Repositories.Interfaces;

public interface ICategoriaRepository
{
    Task<bool> ExisteAsync(string id, CancellationToken ct = default);
    Task<Dictionary<string, string>> ListarNomesPorIdAsync(CancellationToken ct = default);
    Task AdicionarAsync(Categoria categoria, CancellationToken ct = default);
}
