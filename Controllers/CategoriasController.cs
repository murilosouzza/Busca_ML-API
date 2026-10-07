using BuscaML.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuscaML.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly IBuscaService _buscaService;

    public CategoriasController(IBuscaService buscaService)
    {
        _buscaService = buscaService;
    }

    /// GET /api/categorias/MLB1648/mais-vendidos
    /// Devolve os mais vendidos da categoria do Mercado Livre, na ordem do ranking.
    [HttpGet("{id}/mais-vendidos")]
    public async Task<IActionResult> ObterMaisVendidos(string id, CancellationToken ct)
    {
        var resultado = await _buscaService.ObterMaisVendidosDaCategoriaAsync(id, ct);

        if (resultado is null)
        {
            return NotFound(new
            {
                erro = "Não foi possível buscar os mais vendidos dessa categoria e não existe nenhum resultado salvo.",
                id
            });
        }

        return Ok(resultado);
    }
}
