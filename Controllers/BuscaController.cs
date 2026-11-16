using BuscaML.Api.DTOs;
using BuscaML.Api.Services.Exceptions;
using BuscaML.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BuscaML.Api.Controllers;

[ApiController]
[Route("api/buscar")]
public class BuscaController : ControllerBase
{
    private readonly IBuscaService _buscaService;

    public BuscaController(IBuscaService buscaService)
    {
        _buscaService = buscaService;
    }

    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string? termo, CancellationToken ct)
    {
        try
        {
            BuscaResponseDto resultado = await _buscaService.BuscarAsync(termo, ct);
            return Ok(resultado);
        }
        catch (DadoInvalidoException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (NaoEncontradoException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
