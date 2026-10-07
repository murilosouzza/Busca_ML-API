using System.Text.Json;
using BuscaML.Api.Data;
using BuscaML.Api.DTOs;
using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Services;

public class BuscaService : IBuscaService
{
    private readonly AppDbContext _db;
    private readonly IMercadoLivreClient _ml;
    private readonly ILogger<BuscaService> _logger;
    private readonly TimeSpan _ttlCache;

    public BuscaService(AppDbContext db, IMercadoLivreClient ml, IConfiguration config, ILogger<BuscaService> logger)
    {
        _db = db;
        _ml = ml;
        _logger = logger;

        var minutos = config.GetValue<int?>("Cache:TtlMinutosBusca") ?? 30;
        _ttlCache = TimeSpan.FromMinutes(minutos);
    }

    public Task<BuscaResponseDto?> BuscarAsync(string termo, CancellationToken ct = default)
    {
        var termoNormalizado = termo.Trim().ToLowerInvariant();
        return ExecutarBuscaAsync(termoNormalizado, termo, c => _ml.BuscarProdutosAsync(termo, c), ct);
    }

    /// Mais vendidos de uma categoria do Mercado Livre (ex.: "MLB1648"), na ordem do ranking.
    /// Usa o mesmo cache e o mesmo fallback da busca por texto; a chave de cache é "categoria:{id}".
    public Task<BuscaResponseDto?> ObterMaisVendidosDaCategoriaAsync(string categoriaId, CancellationToken ct = default)
    {
        var id = categoriaId.Trim().ToUpperInvariant();
        return ExecutarBuscaAsync($"categoria:{id}", id, c => _ml.BuscarMaisVendidosAsync(id, c), ct);
    }

    /// Fluxo comum: devolve do cache se ainda vale; senão chama o ML, cruza com os rankings,
    /// salva tudo no banco e, se o ML falhar, cai na última busca salva.
    private async Task<BuscaResponseDto?> ExecutarBuscaAsync(
        string termoNormalizado,
        string termo,
        Func<CancellationToken, Task<MlSearchResponseDto>> buscarNoMl,
        CancellationToken ct)
    {
        var ultimaBusca = await _db.Buscas
            .Where(b => b.TermoPesquisado == termoNormalizado)
            .OrderByDescending(b => b.DataHora)
            .FirstOrDefaultAsync(ct);

        var cacheValido = ultimaBusca is not null
            && DateTimeOffset.UtcNow - ultimaBusca.DataHora < _ttlCache;

        if (cacheValido)
        {
            _logger.LogInformation("Cache válido para '{Termo}', devolvendo sem chamar o ML.", termo);
            return await MontarRespostaAPartirDeBuscaAsync(ultimaBusca!, termo, ct);
        }

        try
        {
            var resultadoBusca = await buscarNoMl(ct);

            var categoriasDistintas = resultadoBusca.Results
                .Select(r => r.CategoryId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var posicaoRankingPorProduto = new Dictionary<string, int?>();

            foreach (var categoriaId in categoriasDistintas)
            {
                try
                {
                    var highlights = await ObterHighlightsComCacheAsync(categoriaId!, ct);
                    foreach (var item in highlights.Content)
                        posicaoRankingPorProduto[item.Id] = item.Position;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao buscar /highlights da categoria {CategoriaId}.", categoriaId);
                }
            }

            // Um item é "mais vendido" se o client já trouxe a posição dele (veio de um /highlights)
            // ou se ele aparece no ranking da própria categoria.
            var itensCruzados = resultadoBusca.Results.Select(item => new
            {
                Item = item,
                MaisVendido = item.PosicaoRanking is not null || posicaoRankingPorProduto.ContainsKey(item.Id),
                PosicaoRanking = item.PosicaoRanking
                    ?? (posicaoRankingPorProduto.TryGetValue(item.Id, out var pos) ? pos : null)
            }).ToList();

            foreach (var entrada in itensCruzados)
            {
                await GarantirCategoriaAsync(entrada.Item.CategoryId, ct);
                await GarantirProdutoAsync(entrada.Item, entrada.MaisVendido, entrada.PosicaoRanking, ct);
            }

            var novaBusca = new Busca
            {
                TermoPesquisado = termoNormalizado,
                DataHora = DateTimeOffset.UtcNow
            };
            _db.Buscas.Add(novaBusca);
            await _db.SaveChangesAsync(ct);

            foreach (var entrada in itensCruzados)
            {
                _db.ResultadosBusca.Add(new ResultadoBusca
                {
                    BuscaId = novaBusca.Id,
                    ProdutoIdMl = entrada.Item.Id,
                    PrecoEncontrado = entrada.Item.Price,
                    PosicaoRanking = entrada.PosicaoRanking,
                    RegistradoEm = DateTimeOffset.UtcNow
                });
            }
            await _db.SaveChangesAsync(ct);

            var categoriasMap = await _db.Categorias.ToDictionaryAsync(c => c.Id, c => c.NomeMl, ct);

            var resposta = new BuscaResponseDto
            {
                Termo = termo,
                TotalResultados = itensCruzados.Count,
                Resultados = itensCruzados.Select(x => new ProdutoResumoDto
                {
                    Id = x.Item.Id,
                    Titulo = x.Item.Title,
                    Preco = x.Item.Price,
                    Categoria = x.Item.CategoryId is not null && categoriasMap.TryGetValue(x.Item.CategoryId, out var nome)
                        ? nome
                        : x.Item.CategoryId,
                    MaisVendido = x.MaisVendido,
                    FreteGratis = x.Item.Shipping?.FreeShipping ?? false,
                    ImagemUrl = x.Item.Thumbnail,
                    LinkMl = x.Item.Permalink
                }).ToList()
            };

            return resposta;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao chamar o Mercado Livre para o termo '{Termo}'. Tentando fallback.", termo);

            if (ultimaBusca is not null)
                return await MontarRespostaAPartirDeBuscaAsync(ultimaBusca, termo, ct);

            return null;
        }
    }

    public async Task<ProdutoResumoDto?> ObterProdutoPorIdAsync(string idMl, CancellationToken ct = default)
    {
        var produto = await _db.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.IdMl == idMl, ct);

        if (produto is not null)
            return MapearProdutoParaDto(produto);

        try
        {
            var itemMl = await _ml.ObterItemAsync(idMl, ct);
            if (itemMl is null)
                return null;

            await GarantirCategoriaAsync(itemMl.CategoryId, ct);

            var novoProduto = new Produto
            {
                IdMl = itemMl.Id,
                Titulo = itemMl.Title,
                Preco = itemMl.Price,
                CategoriaId = itemMl.CategoryId,
                AtualizadoEm = DateTimeOffset.UtcNow,
                FreteGratis = itemMl.Shipping?.FreeShipping ?? false,
                ImagemUrl = itemMl.Thumbnail,
                LinkMl = itemMl.Permalink,
                MaisVendido = false,
                PayloadBruto = JsonSerializer.SerializeToDocument(itemMl)
            };

            _db.Produtos.Add(novoProduto);
            await _db.SaveChangesAsync(ct);

            return MapearProdutoParaDto(novoProduto, itemMl.CategoryId);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao buscar produto {IdMl} no Mercado Livre e ele não está em cache.", idMl);
            return null;
        }
    }

    public async Task<BuscaResponseDto> ObterDestaquesAsync(int quantidade, CancellationToken ct = default)
    {
        var produtos = await _db.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.MaisVendido)
            .OrderBy(p => p.PosicaoRanking ?? int.MaxValue)
            .ThenByDescending(p => p.AtualizadoEm)
            .Take(quantidade)
            .ToListAsync(ct);

        return new BuscaResponseDto
        {
            Termo = "destaques",
            TotalResultados = produtos.Count,
            Resultados = produtos.Select(p => MapearProdutoParaDto(p)).ToList()
        };
    }

    private async Task<BuscaResponseDto> MontarRespostaAPartirDeBuscaAsync(Busca busca, string termoExibicao, CancellationToken ct)
    {
        var resultados = await _db.ResultadosBusca
            .Where(r => r.BuscaId == busca.Id)
            .Include(r => r.Produto).ThenInclude(p => p!.Categoria)
            .OrderBy(r => r.Id) // mesma ordem em que os resultados foram salvos
            .ToListAsync(ct);

        return new BuscaResponseDto
        {
            Termo = termoExibicao,
            TotalResultados = resultados.Count,
            Resultados = resultados.Select(r => MapearProdutoParaDto(r.Produto)).ToList()
        };
    }

    private static ProdutoResumoDto MapearProdutoParaDto(Produto produto, string? categoriaIdFallback = null)
    {
        return new ProdutoResumoDto
        {
            Id = produto.IdMl,
            Titulo = produto.Titulo,
            Preco = produto.Preco,
            Categoria = produto.Categoria?.NomeMl ?? categoriaIdFallback,
            MaisVendido = produto.MaisVendido,
            FreteGratis = produto.FreteGratis,
            ImagemUrl = produto.ImagemUrl,
            LinkMl = produto.LinkMl
        };
    }

    private async Task<DTOs.MlHighlightsResponseDto> ObterHighlightsComCacheAsync(string categoriaId, CancellationToken ct)
    {
        var cache = await _db.HighlightsCache.AsNoTracking()
            .FirstOrDefaultAsync(h => h.CategoriaId == categoriaId, ct);

        if (cache is not null)
        {
            try
            {
                var doCache = JsonSerializer.Deserialize<DTOs.MlHighlightsResponseDto>(cache.PayloadBruto.RootElement);
                if (doCache is not null)
                    return doCache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao ler highlights_cache da categoria {CategoriaId}; buscando ao vivo.", categoriaId);
            }
        }

        var highlights = await _ml.ObterHighlightsAsync(categoriaId, ct);

        _db.HighlightsCache.Add(new HighlightCache
        {
            CategoriaId = categoriaId,
            PayloadBruto = JsonSerializer.SerializeToDocument(highlights),
            AtualizadoEm = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return highlights;
    }

    private async Task GarantirCategoriaAsync(string? categoriaId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoriaId))
            return;

        var existe = await _db.Categorias.AnyAsync(c => c.Id == categoriaId, ct);
        if (existe)
            return;

        var nome = categoriaId;
        try
        {
            var categoriaMl = await _ml.ObterCategoriaAsync(categoriaId, ct);
            if (categoriaMl is not null && !string.IsNullOrWhiteSpace(categoriaMl.Name))
                nome = categoriaMl.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao buscar nome da categoria {CategoriaId} no ML; usando o ID como nome por enquanto.", categoriaId);
        }

        _db.Categorias.Add(new Categoria { Id = categoriaId, NomeMl = nome });
        await _db.SaveChangesAsync(ct);
    }

    private async Task GarantirProdutoAsync(DTOs.MlItemDto item, bool maisVendido, int? posicaoRanking, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.IdMl == item.Id, ct);

        if (produto is null)
        {
            produto = new Produto { IdMl = item.Id };
            _db.Produtos.Add(produto);
        }

        produto.Titulo = item.Title;
        produto.Preco = item.Price;
        produto.CategoriaId = item.CategoryId;
        produto.PosicaoRanking = posicaoRanking;
        produto.AtualizadoEm = DateTimeOffset.UtcNow;
        produto.FreteGratis = item.Shipping?.FreeShipping ?? false;
        produto.ImagemUrl = item.Thumbnail;
        produto.LinkMl = item.Permalink;
        produto.MaisVendido = maisVendido;
        produto.PayloadBruto = JsonSerializer.SerializeToDocument(item);

        await _db.SaveChangesAsync(ct);
    }
}
