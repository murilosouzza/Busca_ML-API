using BuscaML.Api.DTOs;
using BuscaML.Api.Services.Exceptions;
using BuscaML.Api.Services.Interfaces;
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
    public async Task<IActionResult> ListarMaisVendidos(string id, CancellationToken ct)
    {
        try
        {
            BuscaResponseDto resultado = await _buscaService.ObterMaisVendidosDaCategoriaAsync(id, ct);
            return Ok(resultado);
        }
        catch (NaoEncontradoException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
