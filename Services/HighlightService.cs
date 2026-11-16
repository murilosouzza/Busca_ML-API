using System.Text.Json;
using BuscaML.Api.Clients.Interfaces;
using BuscaML.Api.DTOs;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using BuscaML.Api.Services.Interfaces;

namespace BuscaML.Api.Services;

public class HighlightService : IHighlightService
{
    private readonly IHighlightCacheRepository _highlightCacheRepository;
    private readonly IMercadoLivreClient _mercadoLivreClient;
    private readonly ILogger<HighlightService> _logger;

    public HighlightService(
        IHighlightCacheRepository highlightCacheRepository,
        IMercadoLivreClient mercadoLivreClient,
        ILogger<HighlightService> logger)
    {
        _highlightCacheRepository = highlightCacheRepository;
        _mercadoLivreClient = mercadoLivreClient;
        _logger = logger;
    }

    public async Task<MlHighlightsResponseDto> ObterComCacheAsync(string categoriaId, CancellationToken ct = default)
    {
        HighlightCache? cache = await _highlightCacheRepository.BuscarPorCategoriaAsync(categoriaId, ct);

        if (cache is not null)
        {
            try
            {
                MlHighlightsResponseDto? doCache = JsonSerializer.Deserialize<MlHighlightsResponseDto>(cache.PayloadBruto.RootElement);
                if (doCache is not null)
                    return doCache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao ler highlights_cache da categoria {CategoriaId}; buscando ao vivo.", categoriaId);
            }
        }

        MlHighlightsResponseDto highlights = await _mercadoLivreClient.ObterHighlightsAsync(categoriaId, ct);
        await SalvarAsync(cache, categoriaId, highlights, ct);

        return highlights;
    }

    public async Task AtualizarAsync(string categoriaId, CancellationToken ct = default)
    {
        MlHighlightsResponseDto highlights = await _mercadoLivreClient.ObterHighlightsAsync(categoriaId, ct);
        HighlightCache? cache = await _highlightCacheRepository.BuscarPorCategoriaAsync(categoriaId, ct);

        await SalvarAsync(cache, categoriaId, highlights, ct);
    }

    /// Cria a linha de cache da categoria ou, se ela já existe, atualiza.
    private async Task SalvarAsync(HighlightCache? cache, string categoriaId, MlHighlightsResponseDto highlights, CancellationToken ct)
    {
        JsonDocument json = JsonSerializer.SerializeToDocument(highlights);

        if (cache is null)
        {
            await _highlightCacheRepository.AdicionarAsync(new HighlightCache
            {
                CategoriaId = categoriaId,
                PayloadBruto = json,
                AtualizadoEm = DateTimeOffset.UtcNow
            }, ct);
            return;
        }

        cache.PayloadBruto = json;
        cache.AtualizadoEm = DateTimeOffset.UtcNow;
        await _highlightCacheRepository.AtualizarAsync(cache, ct);
    }
}
