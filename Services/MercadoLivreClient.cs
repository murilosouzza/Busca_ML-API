using System.Net.Http.Json;
using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services;

public class MercadoLivreClient : IMercadoLivreClient
{
    private readonly HttpClient _http;
    private readonly string _siteId;

    public MercadoLivreClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _siteId = config["MercadoLivre:SiteId"] ?? "MLB";
    }

    public async Task<MlSearchResponseDto> BuscarProdutosAsync(string termo, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/sites/MLB/search?q=fone+bluetooth
        var url = $"/sites/{_siteId}/search?q={Uri.EscapeDataString(termo)}";

        var resposta = await _http.GetFromJsonAsync<MlSearchResponseDto>(url, ct);
        return resposta ?? new MlSearchResponseDto();
    }

    public async Task<MlHighlightsResponseDto> ObterHighlightsAsync(string categoriaId, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/highlights/MLB/category/MLB1051
        var url = $"/highlights/{_siteId}/category/{Uri.EscapeDataString(categoriaId)}";

        var resposta = await _http.GetFromJsonAsync<MlHighlightsResponseDto>(url, ct);
        return resposta ?? new MlHighlightsResponseDto();
    }

    public async Task<MlItemDetailDto?> ObterItemAsync(string idMl, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/items/MLB1234
        var url = $"/items/{Uri.EscapeDataString(idMl)}";

        return await _http.GetFromJsonAsync<MlItemDetailDto>(url, ct);
    }

    public async Task<MlCategoryDto?> ObterCategoriaAsync(string categoriaId, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/categories/MLB1051 -> { "id": "MLB1051", "name": "Eletrônicos, Áudio e Vídeo", ... }
        var url = $"/categories/{Uri.EscapeDataString(categoriaId)}";

        return await _http.GetFromJsonAsync<MlCategoryDto>(url, ct);
    }
}
