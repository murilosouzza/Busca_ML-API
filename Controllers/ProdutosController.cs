using BuscaML.Api.DTOs;
using BuscaML.Api.Services.Exceptions;
using BuscaML.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BuscaML.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtoService;

    public ProdutosController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    [HttpGet("destaques")]
    public async Task<IActionResult> ListarDestaques([FromQuery] int quantidade, CancellationToken ct)
    {
        BuscaResponseDto resultado = await _produtoService.ListarDestaquesAsync(quantidade, ct);
        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(string id, CancellationToken ct)
    {
        try
        {
            ProdutoResumoDto produto = await _produtoService.BuscarPorIdAsync(id, ct);
            return Ok(produto);
        }
        catch (NaoEncontradoException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
