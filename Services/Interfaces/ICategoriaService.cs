namespace BuscaML.Api.Services.Interfaces;

public interface ICategoriaService
{
    /// Garante que a categoria exista no banco (buscando o nome no Mercado Livre se for nova).
    Task GarantirAsync(string? categoriaId, CancellationToken ct = default);
}
