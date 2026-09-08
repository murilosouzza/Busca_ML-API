namespace BuscaML.Api.Models;

public class Busca
{
    public int Id { get; set; }

    // Termo já normalizado (trim + lower) usado como chave de cache.
    public string TermoPesquisado { get; set; } = null!;

    public DateTimeOffset DataHora { get; set; }

    public ICollection<ResultadoBusca> Resultados { get; set; } = new List<ResultadoBusca>();
}
