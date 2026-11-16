using BuscaML.Api.DTOs;
using BuscaML.Api.Models;

namespace BuscaML.Api.Mappers;

public static class ProdutoMapper
{
    /// Converte o produto do banco no formato que a API devolve (contrato com o front).
    public static ProdutoResumoDto ParaResumo(Produto produto, string? categoriaIdFallback = null)
    {
        return new ProdutoResumoDto
        {
            Id = produto.IdMl,
            Titulo = produto.Titulo,
            Preco = produto.Preco,
            Categoria = produto.Categoria?.NomeMl ?? categoriaIdFallback,
            MaisVendido = produto.MaisVendido,
            FreteGratis = produto.FreteGratis,
            ImagemUrl = produto.ImagemUrl,
            LinkMl = produto.LinkMl
        };
    }
}
