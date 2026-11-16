using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services.Interfaces;

public interface IHighlightService
{
    /// Ranking de mais vendidos da categoria: do cache do banco se existir, senão do Mercado Livre.
    Task<MlHighlightsResponseDto> ObterComCacheAsync(string categoriaId, CancellationToken ct = default);

    /// Busca o ranking no Mercado Livre e grava (cria ou atualiza) no cache do banco.
    Task AtualizarAsync(string categoriaId, CancellationToken ct = default);
}
