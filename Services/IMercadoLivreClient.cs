using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services;

public interface IMercadoLivreClient
{
    Task<MlSearchResponseDto> BuscarProdutosAsync(string termo, CancellationToken ct = default);

    Task<MlHighlightsResponseDto> ObterHighlightsAsync(string categoriaId, CancellationToken ct = default);

    Task<MlItemDetailDto?> ObterItemAsync(string idMl, CancellationToken ct = default);

    Task<MlCategoryDto?> ObterCategoriaAsync(string categoriaId, CancellationToken ct = default);
}
