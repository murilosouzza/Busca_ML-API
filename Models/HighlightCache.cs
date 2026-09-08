using System.Text.Json;

namespace BuscaML.Api.Models;

public class HighlightCache
{
    public int Id { get; set; }

    // Categoria do Mercado Livre à qual esse ranking pertence (ex.: "MLB1051").
    public string CategoriaId { get; set; } = null!;

    // Resposta crua de /highlights/{site}/category/{id}.
    public JsonDocument PayloadBruto { get; set; } = null!;

    public DateTimeOffset AtualizadoEm { get; set; }
}
