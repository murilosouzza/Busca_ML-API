using System.Text.Json.Serialization;

namespace BuscaML.Api.DTOs;
public class MlSearchResponseDto
{
    [JsonPropertyName("results")]
    public List<MlItemDto> Results { get; set; } = new();
}

public class MlItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("shipping")]
    public MlShippingDto? Shipping { get; set; }

    /// Posição no ranking de mais vendidos, quando o item veio de um /highlights.
    /// Não é um campo do Mercado Livre: quem preenche é o nosso client. Null = não é mais vendido conhecido.
    [JsonPropertyName("posicao_ranking")]
    public int? PosicaoRanking { get; set; }
}

public class MlShippingDto
{
    [JsonPropertyName("free_shipping")]
    public bool FreeShipping { get; set; }
}

public class MlHighlightsResponseDto
{
    [JsonPropertyName("content")]
    public List<MlHighlightItemDto> Content { get; set; } = new();
}

public class MlHighlightItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("position")]
    public int? Position { get; set; }
}

public class MlCategoryDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

public class MlItemDetailDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("shipping")]
    public MlShippingDto? Shipping { get; set; }
}
