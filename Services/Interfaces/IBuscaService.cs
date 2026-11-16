using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services.Interfaces;

public interface IBuscaService
{
    Task<BuscaResponseDto> BuscarAsync(string? termo, CancellationToken ct = default);
    Task<BuscaResponseDto> ObterMaisVendidosDaCategoriaAsync(string categoriaId, CancellationToken ct = default);
}
