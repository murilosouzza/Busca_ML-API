using BuscaML.Api.Clients.Interfaces;
using BuscaML.Api.DTOs;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using BuscaML.Api.Services.Interfaces;

namespace BuscaML.Api.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IMercadoLivreClient _mercadoLivreClient;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(
        ICategoriaRepository categoriaRepository,
        IMercadoLivreClient mercadoLivreClient,
        ILogger<CategoriaService> logger)
    {
        _categoriaRepository = categoriaRepository;
        _mercadoLivreClient = mercadoLivreClient;
        _logger = logger;
    }

    public async Task GarantirAsync(string? categoriaId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(categoriaId))
            return;

        if (await _categoriaRepository.ExisteAsync(categoriaId, ct))
            return;

        string nome = categoriaId;
        try
        {
            MlCategoryDto? categoriaMl = await _mercadoLivreClient.ObterCategoriaAsync(categoriaId, ct);
            if (categoriaMl is not null && !string.IsNullOrWhiteSpace(categoriaMl.Name))
                nome = categoriaMl.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao buscar nome da categoria {CategoriaId} no ML; usando o ID como nome por enquanto.", categoriaId);
        }

        await _categoriaRepository.AdicionarAsync(new Categoria { Id = categoriaId, NomeMl = nome }, ct);
    }
}
