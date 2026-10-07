using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services;

/// <summary>
/// Cliente da API oficial do Mercado Livre.
///
/// O endpoint clássico /sites/MLB/search responde 403 para a nossa aplicação, então a busca
/// é montada com os endpoints de catálogo, que funcionam com o token client_credentials:
///   /products/search        -> acha produtos pelo termo (sem preço)
///   /products/{id}/items    -> anúncios do produto (de onde vêm preço, frete e categoria)
///   /highlights/...         -> mais vendidos da categoria
///
/// Os métodos devolvem os mesmos DTOs de antes, então BuscaService, FakeMercadoLivreClient e o
/// contrato com o front não mudam. O "id" de cada resultado passa a ser o id do produto de
/// catálogo (ex.: MLB70040749), que é o mesmo id que aparece no /highlights.
/// </summary>
public class MercadoLivreClient : IMercadoLivreClient
{
    private const int LimiteResultados = 20;
    private const int ChamadasSimultaneas = 5;

    private readonly HttpClient _http;
    private readonly ILogger<MercadoLivreClient> _logger;
    private readonly string _siteId;

    public MercadoLivreClient(HttpClient http, IConfiguration config, ILogger<MercadoLivreClient> logger)
    {
        _http = http;
        _logger = logger;
        _siteId = config["MercadoLivre:SiteId"] ?? "MLB";
    }

    public async Task<MlSearchResponseDto> BuscarProdutosAsync(string termo, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q=notebook&limit=20
        var url = $"/products/search?status=active&site_id={_siteId}&q={Uri.EscapeDataString(termo)}&limit={LimiteResultados}";
        var catalogo = await GetOuNuloAsync<CatalogoBuscaDto>(url, ct);

        var produtos = (catalogo?.Results ?? new List<CatalogoProdutoDto>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        // O catálogo lista produto mesmo sem ninguém vendendo; ficamos só com quem tem anúncio ativo.
        var itens = await EmParaleloAsync(produtos, async produto =>
        {
            var oferta = await ObterMelhorOfertaAsync(produto.Id, ct);
            return oferta is null ? null : MontarItem(produto, oferta);
        }, ct);

        // Costuma sobrar pouco resultado, então completamos com os mais vendidos da categoria do termo.
        if (itens.Count < LimiteResultados)
        {
            var complemento = await BuscarMaisVendidosDaCategoriaDoTermoAsync(termo, itens, ct);
            itens.AddRange(complemento.Take(LimiteResultados - itens.Count));
        }

        return new MlSearchResponseDto { Results = itens };
    }

    public async Task<MlHighlightsResponseDto> ObterHighlightsAsync(string categoriaId, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/highlights/MLB/category/MLB1652
        var url = $"/highlights/{_siteId}/category/{Uri.EscapeDataString(categoriaId)}";

        var resposta = await _http.GetFromJsonAsync<MlHighlightsResponseDto>(url, ct);
        return resposta ?? new MlHighlightsResponseDto();
    }

    public async Task<MlSearchResponseDto> BuscarMaisVendidosAsync(string categoriaId, CancellationToken ct = default)
    {
        var itens = await CarregarMaisVendidosAsync(categoriaId, new HashSet<string>(), ct);
        return new MlSearchResponseDto { Results = itens };
    }

    public async Task<MlItemDetailDto?> ObterItemAsync(string idMl, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/products/MLB70040749  +  /products/MLB70040749/items
        var produto = await ObterProdutoCatalogoAsync(idMl, ct);
        if (produto is null)
            return null;

        // Sem anúncio ativo não há preço pra mostrar; tratamos como produto indisponível.
        var oferta = await ObterMelhorOfertaAsync(idMl, ct);
        if (oferta is null)
            return null;

        var item = MontarItem(produto, oferta);
        return new MlItemDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Price = item.Price,
            Thumbnail = item.Thumbnail,
            Permalink = item.Permalink,
            CategoryId = item.CategoryId,
            Shipping = item.Shipping
        };
    }

    public async Task<MlCategoryDto?> ObterCategoriaAsync(string categoriaId, CancellationToken ct = default)
    {
        // https://api.mercadolibre.com/categories/MLB1051 -> { "id": "MLB1051", "name": "Eletrônicos, Áudio e Vídeo", ... }
        var url = $"/categories/{Uri.EscapeDataString(categoriaId)}";

        return await _http.GetFromJsonAsync<MlCategoryDto>(url, ct);
    }

    // ---------------------------------------------------------------------------------------
    // Complemento com os mais vendidos
    // ---------------------------------------------------------------------------------------

    /// Descobre a categoria mais provável do termo e devolve os mais vendidos dela (com preço),
    /// colocando primeiro os que têm o termo no nome. Qualquer falha aqui só deixa a busca sem
    /// complemento; não derruba a busca.
    private async Task<List<MlItemDto>> BuscarMaisVendidosDaCategoriaDoTermoAsync(
        string termo, List<MlItemDto> jaEncontrados, CancellationToken ct)
    {
        try
        {
            // https://api.mercadolibre.com/sites/MLB/domain_discovery/search?limit=1&q=notebook
            var urlCategoria = $"/sites/{_siteId}/domain_discovery/search?limit=1&q={Uri.EscapeDataString(termo)}";
            var categorias = await GetOuNuloAsync<List<CategoriaDescobertaDto>>(urlCategoria, ct);
            var categoriaId = categorias?.FirstOrDefault()?.CategoryId;

            if (string.IsNullOrWhiteSpace(categoriaId))
                return new List<MlItemDto>();

            var idsJaEncontrados = jaEncontrados.Select(i => i.Id).ToHashSet();
            var candidatos = await CarregarMaisVendidosAsync(categoriaId, idsJaEncontrados, ct);

            // OrderBy é estável: dentro de cada grupo a ordem do ranking é mantida.
            var palavras = termo.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return candidatos
                .OrderByDescending(c => palavras.All(p => c.Title.Contains(p, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha ao complementar a busca de '{Termo}' com os mais vendidos.", termo);
            return new List<MlItemDto>();
        }
    }

    /// Lê o /highlights da categoria e carrega cada produto (ficha + anúncio mais barato),
    /// na ordem do ranking e já com a posição preenchida. Produto sem anúncio ativo fica de fora.
    private async Task<List<MlItemDto>> CarregarMaisVendidosAsync(
        string categoriaId, ISet<string> idsParaIgnorar, CancellationToken ct)
    {
        // https://api.mercadolibre.com/highlights/MLB/category/MLB1652
        var url = $"/highlights/{_siteId}/category/{Uri.EscapeDataString(categoriaId)}";
        var highlights = await GetOuNuloAsync<HighlightsDto>(url, ct);

        // No ranking vêm produtos de catálogo ("PRODUCT") e anúncios avulsos ("ITEM").
        // Só os de catálogo têm os endpoints /products/{id} que conseguimos acessar.
        var ranking = (highlights?.Content ?? new List<HighlightDto>())
            .Where(h => !string.IsNullOrWhiteSpace(h.Id))
            .Where(h => string.IsNullOrEmpty(h.Type) || h.Type.Equals("PRODUCT", StringComparison.OrdinalIgnoreCase))
            .Where(h => !idsParaIgnorar.Contains(h.Id))
            .GroupBy(h => h.Id)
            .Select(g => g.First())
            .ToList();

        return await EmParaleloAsync(ranking, async destaque =>
        {
            var produto = await ObterProdutoCatalogoAsync(destaque.Id, ct);
            if (produto is null)
                return null;

            var oferta = await ObterMelhorOfertaAsync(destaque.Id, ct);
            if (oferta is null)
                return null;

            var item = MontarItem(produto, oferta);
            item.PosicaoRanking = destaque.Position ?? (ranking.IndexOf(destaque) + 1);
            return item;
        }, ct);
    }

    // ---------------------------------------------------------------------------------------
    // Chamadas de catálogo
    // ---------------------------------------------------------------------------------------

    private Task<CatalogoProdutoDto?> ObterProdutoCatalogoAsync(string idProduto, CancellationToken ct)
    {
        return GetOuNuloAsync<CatalogoProdutoDto>($"/products/{Uri.EscapeDataString(idProduto)}", ct);
    }

    /// Anúncio mais barato do produto. Null quando ninguém está vendendo (o ML responde 404 "No winners found").
    private async Task<OfertaDto?> ObterMelhorOfertaAsync(string idProduto, CancellationToken ct)
    {
        var url = $"/products/{Uri.EscapeDataString(idProduto)}/items?limit=1";

        var resposta = await GetOuNuloAsync<OfertasDto>(url, ct);
        return resposta?.Results.FirstOrDefault(o => o.Price is not null);
    }

    /// GET que devolve null em 404 e lança HttpRequestException nos outros erros (401, 403, 500...).
    private async Task<T?> GetOuNuloAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var resposta = await _http.GetAsync(url, ct);

        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;

        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    /// Roda a função para cada entrada, no máximo ChamadasSimultaneas por vez, mantendo a ordem.
    /// Entrada que falhar ou devolver null fica de fora do resultado.
    private async Task<List<MlItemDto>> EmParaleloAsync<TEntrada>(
        IReadOnlyList<TEntrada> entradas, Func<TEntrada, Task<MlItemDto?>> funcao, CancellationToken ct)
    {
        using var limitador = new SemaphoreSlim(ChamadasSimultaneas);

        var tarefas = entradas.Select(async entrada =>
        {
            await limitador.WaitAsync(ct);
            try
            {
                return await funcao(entrada);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Falha em uma chamada ao Mercado Livre; item ignorado.");
                return null;
            }
            finally
            {
                limitador.Release();
            }
        }).ToList();

        var resultados = await Task.WhenAll(tarefas);

        var itens = new List<MlItemDto>();
        foreach (var item in resultados)
        {
            if (item is not null)
                itens.Add(item);
        }
        return itens;
    }

    private static MlItemDto MontarItem(CatalogoProdutoDto produto, OfertaDto oferta)
    {
        return new MlItemDto
        {
            Id = produto.Id,
            Title = string.IsNullOrWhiteSpace(produto.Name) ? produto.Id : produto.Name,
            Price = oferta.Price ?? 0m,
            Thumbnail = produto.Pictures?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Url))?.Url,
            // O permalink costuma vir vazio na API de catálogo; a página do produto segue o padrão /p/{id}.
            Permalink = string.IsNullOrWhiteSpace(produto.Permalink)
                ? $"https://www.mercadolivre.com.br/p/{produto.Id}"
                : produto.Permalink,
            CategoryId = string.IsNullOrWhiteSpace(oferta.CategoryId) ? null : oferta.CategoryId,
            Shipping = new MlShippingDto { FreeShipping = oferta.Shipping?.FreeShipping ?? false }
        };
    }

    // ---------------------------------------------------------------------------------------
    // Formato bruto das respostas de catálogo (só os campos que usamos)
    // ---------------------------------------------------------------------------------------

    private sealed class CatalogoBuscaDto
    {
        [JsonPropertyName("results")]
        public List<CatalogoProdutoDto> Results { get; set; } = new();
    }

    private sealed class CatalogoProdutoDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; set; }

        [JsonPropertyName("pictures")]
        public List<FotoDto>? Pictures { get; set; }
    }

    private sealed class FotoDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private sealed class OfertasDto
    {
        [JsonPropertyName("results")]
        public List<OfertaDto> Results { get; set; } = new();
    }

    private sealed class OfertaDto
    {
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("category_id")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("shipping")]
        public OfertaFreteDto? Shipping { get; set; }
    }

    private sealed class OfertaFreteDto
    {
        [JsonPropertyName("free_shipping")]
        public bool? FreeShipping { get; set; }
    }

    private sealed class HighlightsDto
    {
        [JsonPropertyName("content")]
        public List<HighlightDto> Content { get; set; } = new();
    }

    private sealed class HighlightDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    private sealed class CategoriaDescobertaDto
    {
        [JsonPropertyName("category_id")]
        public string? CategoryId { get; set; }
    }
}
