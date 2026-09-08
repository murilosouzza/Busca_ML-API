using BuscaML.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuscaML.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly IBuscaService _buscaService;

    public ProdutosController(IBuscaService buscaService)
    {
        _buscaService = buscaService;
    }

    [HttpGet("destaques")]
    public async Task<IActionResult> ObterDestaques([FromQuery] int quantidade, CancellationToken ct)
    {
        var qtd = quantidade > 0 ? quantidade : 10;
        var resultado = await _buscaService.ObterDestaquesAsync(qtd, ct);
        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(string id, CancellationToken ct)
    {
        var produto = await _buscaService.ObterProdutoPorIdAsync(id, ct);

        if (produto is null)
        {
            return NotFound(new { erro = "Produto não encontrado.", id });
        }

        return Ok(produto);
    }
}
