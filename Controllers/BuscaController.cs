using BuscaML.Api.Services;
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
        if (string.IsNullOrWhiteSpace(termo))
        {
            return BadRequest(new { erro = "O parâmetro 'termo' é obrigatório." });
        }

        var resultado = await _buscaService.BuscarAsync(termo, ct);

        if (resultado is null)
        {
            return NotFound(new
            {
                erro = "Não foi possível buscar esse termo e não existe nenhum resultado salvo.",
                termo
            });
        }

        return Ok(resultado);
    }
}
