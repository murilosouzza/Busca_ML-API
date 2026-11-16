using BuscaML.Api.Models;

namespace BuscaML.Api.Repositories.Interfaces;

public interface IProdutoRepository
{
    Task<Produto?> BuscarPorIdMlAsync(string idMl, CancellationToken ct = default);
    Task<Produto?> BuscarPorIdMlComCategoriaAsync(string idMl, CancellationToken ct = default);
    Task<List<Produto>> ListarDestaquesAsync(int quantidade, CancellationToken ct = default);
    Task<List<string>> ListarCategoriaIdsAsync(CancellationToken ct = default);
    Task<bool> ExisteAlgumAsync(CancellationToken ct = default);
    Task AdicionarAsync(Produto produto, CancellationToken ct = default);
    Task AtualizarAsync(Produto produto, CancellationToken ct = default);
}
