using System.Text.Json;

namespace BuscaML.Api.Models;

public class Produto
{
    // O IdMl (ex.: "MLB1234") é a chave primária: é o que o resto do sistema usa para referenciar o produto.
    public string IdMl { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public decimal Preco { get; set; }

    public string? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int? PosicaoRanking { get; set; }

    public DateTimeOffset AtualizadoEm { get; set; }

    public bool FreteGratis { get; set; }

    public string? ImagemUrl { get; set; }

    public string? LinkMl { get; set; }

    public bool MaisVendido { get; set; }

    // Payload cru retornado pelo Mercado Livre, guardado para auditoria / reprocessamento.
    public JsonDocument PayloadBruto { get; set; } = null!;
}
