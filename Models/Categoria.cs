namespace BuscaML.Api.Models;

public class Categoria
{
    // O Id é o próprio identificador de categoria do Mercado Livre (ex.: "MLB1051").
    public string Id { get; set; } = null!;

    public string NomeMl { get; set; } = null!;

    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}
