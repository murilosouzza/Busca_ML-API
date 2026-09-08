using System.Text.Json.Serialization;

namespace BuscaML.Api.DTOs;

/// Resposta de GET /api/buscar?termo=X. Os nomes dos campos JSON aqui são o contrato combinado com o front.
public class BuscaResponseDto
{
    [JsonPropertyName("termo")]
    public string Termo { get; set; } = null!;

    [JsonPropertyName("total_resultados")]
    public int TotalResultados { get; set; }

    [JsonPropertyName("resultados")]
    public List<ProdutoResumoDto> Resultados { get; set; } = new();
}

/// Um item dentro da lista de resultados (e também é o formato usado em GET /api/produtos/{id})
public class ProdutoResumoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = null!;

    [JsonPropertyName("preco")]
    public decimal? Preco { get; set; }

    [JsonPropertyName("categoria")]
    public string? Categoria { get; set; }

    [JsonPropertyName("mais_vendido")]
    public bool MaisVendido { get; set; }

    [JsonPropertyName("frete_gratis")]
    public bool FreteGratis { get; set; }

    [JsonPropertyName("imagem_url")]
    public string? ImagemUrl { get; set; }

    [JsonPropertyName("link_ml")]
    public string? LinkMl { get; set; }
}
