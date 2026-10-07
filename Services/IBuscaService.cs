using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services;

public interface IBuscaService
{
    Task<BuscaResponseDto?> BuscarAsync(string termo, CancellationToken ct = default);
    Task<ProdutoResumoDto?> ObterProdutoPorIdAsync(string idMl, CancellationToken ct = default);
    Task<BuscaResponseDto> ObterDestaquesAsync(int quantidade, CancellationToken ct = default);
    Task<BuscaResponseDto?> ObterMaisVendidosDaCategoriaAsync(string categoriaId, CancellationToken ct = default);
}
