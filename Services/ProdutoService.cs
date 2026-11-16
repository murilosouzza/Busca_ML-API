using System.Text.Json;
using BuscaML.Api.Clients.Interfaces;
using BuscaML.Api.DTOs;
using BuscaML.Api.Mappers;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using BuscaML.Api.Services.Exceptions;
using BuscaML.Api.Services.Interfaces;

namespace BuscaML.Api.Services;

public class ProdutoService : IProdutoService
{
    private const int QuantidadePadraoDestaques = 10;

    private readonly IProdutoRepository _produtoRepository;
    private readonly ICategoriaService _categoriaService;
    private readonly IMercadoLivreClient _mercadoLivreClient;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(
        IProdutoRepository produtoRepository,
        ICategoriaService categoriaService,
        IMercadoLivreClient mercadoLivreClient,
        ILogger<ProdutoService> logger)
    {
        _produtoRepository = produtoRepository;
        _categoriaService = categoriaService;
        _mercadoLivreClient = mercadoLivreClient;
        _logger = logger;
    }

    public async Task<ProdutoResumoDto> BuscarPorIdAsync(string idMl, CancellationToken ct = default)
    {
        Produto? produto = await _produtoRepository.BuscarPorIdMlComCategoriaAsync(idMl, ct);

        if (produto is not null)
            return ProdutoMapper.ParaResumo(produto);

        try
        {
            MlItemDetailDto? itemMl = await _mercadoLivreClient.ObterItemAsync(idMl, ct);
            if (itemMl is null)
                throw new NaoEncontradoException("Produto não encontrado.");

            await _categoriaService.GarantirAsync(itemMl.CategoryId, ct);

            Produto novoProduto = new Produto
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

            await _produtoRepository.AdicionarAsync(novoProduto, ct);

            return ProdutoMapper.ParaResumo(novoProduto, itemMl.CategoryId);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao buscar produto {IdMl} no Mercado Livre e ele não está em cache.", idMl);
            throw new NaoEncontradoException("Produto não encontrado.");
        }
    }

    public async Task<BuscaResponseDto> ListarDestaquesAsync(int quantidade, CancellationToken ct = default)
    {
        int quantidadeFinal = quantidade > 0 ? quantidade : QuantidadePadraoDestaques;

        List<Produto> produtos = await _produtoRepository.ListarDestaquesAsync(quantidadeFinal, ct);

        return new BuscaResponseDto
        {
            Termo = "destaques",
            TotalResultados = produtos.Count,
            Resultados = produtos.Select(p => ProdutoMapper.ParaResumo(p)).ToList()
        };
    }
}
