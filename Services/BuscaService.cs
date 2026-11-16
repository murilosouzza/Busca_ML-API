using System.Text.Json;
using BuscaML.Api.Clients.Interfaces;
using BuscaML.Api.DTOs;
using BuscaML.Api.Mappers;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using BuscaML.Api.Services.Exceptions;
using BuscaML.Api.Services.Interfaces;

namespace BuscaML.Api.Services;

public class BuscaService : IBuscaService
{
    private const string MensagemSemResultadoBusca =
        "Não foi possível buscar esse termo e não existe nenhum resultado salvo.";

    private const string MensagemSemResultadoCategoria =
        "Não foi possível buscar os mais vendidos dessa categoria e não existe nenhum resultado salvo.";

    private readonly IBuscaRepository _buscaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ICategoriaService _categoriaService;
    private readonly IHighlightService _highlightService;
    private readonly IMercadoLivreClient _mercadoLivreClient;
    private readonly ILogger<BuscaService> _logger;
    private readonly TimeSpan _ttlCache;

    public BuscaService(
        IBuscaRepository buscaRepository,
        IProdutoRepository produtoRepository,
        ICategoriaRepository categoriaRepository,
        ICategoriaService categoriaService,
        IHighlightService highlightService,
        IMercadoLivreClient mercadoLivreClient,
        IConfiguration config,
        ILogger<BuscaService> logger)
    {
        _buscaRepository = buscaRepository;
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _categoriaService = categoriaService;
        _highlightService = highlightService;
        _mercadoLivreClient = mercadoLivreClient;
        _logger = logger;

        int minutos = config.GetValue<int?>("Cache:TtlMinutosBusca") ?? 30;
        _ttlCache = TimeSpan.FromMinutes(minutos);
    }

    public async Task<BuscaResponseDto> BuscarAsync(string? termo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(termo))
            throw new DadoInvalidoException("O parâmetro 'termo' é obrigatório.");

        string termoOriginal = termo;
        string termoNormalizado = termoOriginal.Trim().ToLowerInvariant();

        return await ExecutarBuscaAsync(
            termoNormalizado,
            termoOriginal,
            c => _mercadoLivreClient.BuscarProdutosAsync(termoOriginal, c),
            MensagemSemResultadoBusca,
            ct);
    }

    /// Mais vendidos de uma categoria do Mercado Livre (ex.: "MLB1648"), na ordem do ranking.
    /// Usa o mesmo cache e o mesmo fallback da busca por texto; a chave de cache é "categoria:{id}".
    public async Task<BuscaResponseDto> ObterMaisVendidosDaCategoriaAsync(string categoriaId, CancellationToken ct = default)
    {
        string id = categoriaId.Trim().ToUpperInvariant();

        return await ExecutarBuscaAsync(
            $"categoria:{id}",
            id,
            c => _mercadoLivreClient.BuscarMaisVendidosAsync(id, c),
            MensagemSemResultadoCategoria,
            ct);
    }

    /// Fluxo comum: devolve do cache se ainda vale; senão chama o ML, cruza com os rankings,
    /// salva tudo no banco e, se o ML falhar, cai na última busca salva.
    private async Task<BuscaResponseDto> ExecutarBuscaAsync(
        string termoNormalizado,
        string termo,
        Func<CancellationToken, Task<MlSearchResponseDto>> buscarNoMl,
        string mensagemSemResultado,
        CancellationToken ct)
    {
        Busca? ultimaBusca = await _buscaRepository.BuscarUltimaPorTermoAsync(termoNormalizado, ct);

        bool cacheValido = ultimaBusca is not null
            && DateTimeOffset.UtcNow - ultimaBusca.DataHora < _ttlCache;

        if (cacheValido)
        {
            _logger.LogInformation("Cache válido para '{Termo}', devolvendo sem chamar o ML.", termo);
            return await MontarRespostaAPartirDeBuscaAsync(ultimaBusca!, termo, ct);
        }

        try
        {
            return await BuscarNoMercadoLivreAsync(termoNormalizado, termo, buscarNoMl, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao chamar o Mercado Livre para o termo '{Termo}'. Tentando fallback.", termo);

            if (ultimaBusca is not null)
                return await MontarRespostaAPartirDeBuscaAsync(ultimaBusca, termo, ct);

            throw new NaoEncontradoException(mensagemSemResultado);
        }
    }

    private async Task<BuscaResponseDto> BuscarNoMercadoLivreAsync(
        string termoNormalizado,
        string termo,
        Func<CancellationToken, Task<MlSearchResponseDto>> buscarNoMl,
        CancellationToken ct)
    {
        MlSearchResponseDto resultadoBusca = await buscarNoMl(ct);

        List<string?> categoriasDistintas = resultadoBusca.Results
            .Select(r => r.CategoryId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        Dictionary<string, int?> posicaoRankingPorProduto = new Dictionary<string, int?>();

        foreach (string? categoriaId in categoriasDistintas)
        {
            try
            {
                MlHighlightsResponseDto highlights = await _highlightService.ObterComCacheAsync(categoriaId!, ct);
                foreach (MlHighlightItemDto item in highlights.Content)
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
            await _categoriaService.GarantirAsync(entrada.Item.CategoryId, ct);
            await GarantirProdutoAsync(entrada.Item, entrada.MaisVendido, entrada.PosicaoRanking, ct);
        }

        Busca novaBusca = new Busca
        {
            TermoPesquisado = termoNormalizado,
            DataHora = DateTimeOffset.UtcNow
        };
        await _buscaRepository.AdicionarAsync(novaBusca, ct);

        List<ResultadoBusca> resultados = itensCruzados.Select(entrada => new ResultadoBusca
        {
            BuscaId = novaBusca.Id,
            ProdutoIdMl = entrada.Item.Id,
            PrecoEncontrado = entrada.Item.Price,
            PosicaoRanking = entrada.PosicaoRanking,
            RegistradoEm = DateTimeOffset.UtcNow
        }).ToList();
        await _buscaRepository.AdicionarResultadosAsync(resultados, ct);

        Dictionary<string, string> categoriasMap = await _categoriaRepository.ListarNomesPorIdAsync(ct);

        return new BuscaResponseDto
        {
            Termo = termo,
            TotalResultados = itensCruzados.Count,
            Resultados = itensCruzados.Select(x => new ProdutoResumoDto
            {
                Id = x.Item.Id,
                Titulo = x.Item.Title,
                Preco = x.Item.Price,
                Categoria = x.Item.CategoryId is not null && categoriasMap.TryGetValue(x.Item.CategoryId, out string? nome)
                    ? nome
                    : x.Item.CategoryId,
                MaisVendido = x.MaisVendido,
                FreteGratis = x.Item.Shipping?.FreeShipping ?? false,
                ImagemUrl = x.Item.Thumbnail,
                LinkMl = x.Item.Permalink
            }).ToList()
        };
    }

    private async Task<BuscaResponseDto> MontarRespostaAPartirDeBuscaAsync(Busca busca, string termoExibicao, CancellationToken ct)
    {
        List<ResultadoBusca> resultados = await _buscaRepository.ListarResultadosAsync(busca.Id, ct);

        return new BuscaResponseDto
        {
            Termo = termoExibicao,
            TotalResultados = resultados.Count,
            // O produto é obrigatório no resultado (chave estrangeira), por isso o "!".
            Resultados = resultados.Select(r => ProdutoMapper.ParaResumo(r.Produto!)).ToList()
        };
    }

    private async Task GarantirProdutoAsync(MlItemDto item, bool maisVendido, int? posicaoRanking, CancellationToken ct)
    {
        Produto? existente = await _produtoRepository.BuscarPorIdMlAsync(item.Id, ct);
        Produto produto = existente ?? new Produto { IdMl = item.Id };

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

        if (existente is null)
            await _produtoRepository.AdicionarAsync(produto, ct);
        else
            await _produtoRepository.AtualizarAsync(produto, ct);
    }
}
