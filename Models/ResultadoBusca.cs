namespace BuscaML.Api.Models;

public class ResultadoBusca
{
    public int Id { get; set; }

    public int BuscaId { get; set; }
    public Busca? Busca { get; set; }

    // Aponta para Produto.IdMl.
    public string ProdutoIdMl { get; set; } = null!;
    public Produto? Produto { get; set; }

    public decimal PrecoEncontrado { get; set; }

    public int? PosicaoRanking { get; set; }

    public DateTimeOffset RegistradoEm { get; set; }
}
