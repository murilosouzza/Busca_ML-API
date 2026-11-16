using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services.Interfaces;

public interface IProdutoService
{
    Task<ProdutoResumoDto> BuscarPorIdAsync(string idMl, CancellationToken ct = default);
    Task<BuscaResponseDto> ListarDestaquesAsync(int quantidade, CancellationToken ct = default);
}
