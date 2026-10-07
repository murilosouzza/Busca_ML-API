using BuscaML.Api.DTOs;

namespace BuscaML.Api.Services;

/// <summary>
/// Implementação de <see cref="IMercadoLivreClient"/> com dados fixos, para desenvolvimento e
/// demonstração sem depender de credencial nem de rede. Ativada por MercadoLivre:UseFake=true.
/// Cobre os mesmos 4 métodos da API real, então cache, highlights, background service e
/// endpoints funcionam de ponta a ponta.
/// </summary>
public class FakeMercadoLivreClient : IMercadoLivreClient
{
    private const string CategoriaCelulares = "MLB1051";
    private const string CategoriaAudio = "MLB1000";

    private static readonly Dictionary<string, string> CategoriasPorId = new()
    {
        [CategoriaCelulares] = "Celulares e Telefones",
        [CategoriaAudio] = "Eletrônicos, Áudio e Vídeo"
    };

    // Ranking de "mais vendidos" por categoria (id do produto -> posição).
    private static readonly Dictionary<string, List<MlHighlightItemDto>> HighlightsPorCategoria = new()
    {
        [CategoriaAudio] = new()
        {
            new MlHighlightItemDto { Id = "MLB-FAKE-1", Position = 1 },
            new MlHighlightItemDto { Id = "MLB-FAKE-3", Position = 2 }
        },
        [CategoriaCelulares] = new()
        {
            new MlHighlightItemDto { Id = "MLB-FAKE-2", Position = 1 }
        }
    };

    public Task<MlSearchResponseDto> BuscarProdutosAsync(string termo, CancellationToken ct = default)
    {
        var termoLimpo = string.IsNullOrWhiteSpace(termo) ? "produto" : termo.Trim();

        var resultado = new MlSearchResponseDto
        {
            Results = new List<MlItemDto>
            {
                new()
                {
                    Id = "MLB-FAKE-1",
                    Title = $"{termoLimpo} - Fone Bluetooth TWS (fake)",
                    Price = 199.90m,
                    Thumbnail = "https://http2.mlstatic.com/fake/fone.jpg",
                    Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-1",
                    CategoryId = CategoriaAudio,
                    Shipping = new MlShippingDto { FreeShipping = true }
                },
                new()
                {
                    Id = "MLB-FAKE-2",
                    Title = $"{termoLimpo} - Smartphone 128GB (fake)",
                    Price = 1499.00m,
                    Thumbnail = "https://http2.mlstatic.com/fake/celular.jpg",
                    Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-2",
                    CategoryId = CategoriaCelulares,
                    Shipping = new MlShippingDto { FreeShipping = true }
                },
                new()
                {
                    Id = "MLB-FAKE-3",
                    Title = $"{termoLimpo} - Caixa de Som Portátil (fake)",
                    Price = 349.99m,
                    Thumbnail = "https://http2.mlstatic.com/fake/caixa.jpg",
                    Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-3",
                    CategoryId = CategoriaAudio,
                    Shipping = new MlShippingDto { FreeShipping = false }
                },
                new()
                {
                    Id = "MLB-FAKE-4",
                    Title = $"{termoLimpo} - Cabo USB-C 2m (fake)",
                    Price = 29.90m,
                    Thumbnail = "https://http2.mlstatic.com/fake/cabo.jpg",
                    Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-4",
                    CategoryId = CategoriaAudio,
                    Shipping = new MlShippingDto { FreeShipping = false }
                }
            }
        };

        return Task.FromResult(resultado);
    }

    public Task<MlHighlightsResponseDto> ObterHighlightsAsync(string categoriaId, CancellationToken ct = default)
    {
        var content = HighlightsPorCategoria.TryGetValue(categoriaId, out var lista)
            ? lista
            : new List<MlHighlightItemDto>();

        return Task.FromResult(new MlHighlightsResponseDto { Content = content });
    }

    public async Task<MlSearchResponseDto> BuscarMaisVendidosAsync(string categoriaId, CancellationToken ct = default)
    {
        // Reaproveita os itens fixos da busca e fica só com os que estão no ranking da categoria.
        var todos = await BuscarProdutosAsync("Mais vendido", ct);
        var ranking = HighlightsPorCategoria.TryGetValue(categoriaId, out var lista)
            ? lista
            : new List<MlHighlightItemDto>();

        var itens = new List<MlItemDto>();
        foreach (var destaque in ranking)
        {
            var item = todos.Results.FirstOrDefault(i => i.Id == destaque.Id);
            if (item is null)
                continue;

            item.PosicaoRanking = destaque.Position;
            itens.Add(item);
        }

        return new MlSearchResponseDto { Results = itens };
    }

    public Task<MlItemDetailDto?> ObterItemAsync(string idMl, CancellationToken ct = default)
    {
        MlItemDetailDto? item = idMl switch
        {
            "MLB-FAKE-1" => new MlItemDetailDto
            {
                Id = idMl,
                Title = "Fone Bluetooth TWS (fake)",
                Price = 199.90m,
                Thumbnail = "https://http2.mlstatic.com/fake/fone.jpg",
                Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-1",
                CategoryId = CategoriaAudio,
                Shipping = new MlShippingDto { FreeShipping = true }
            },
            "MLB-FAKE-2" => new MlItemDetailDto
            {
                Id = idMl,
                Title = "Smartphone 128GB (fake)",
                Price = 1499.00m,
                Thumbnail = "https://http2.mlstatic.com/fake/celular.jpg",
                Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-2",
                CategoryId = CategoriaCelulares,
                Shipping = new MlShippingDto { FreeShipping = true }
            },
            "MLB-FAKE-3" => new MlItemDetailDto
            {
                Id = idMl,
                Title = "Caixa de Som Portátil (fake)",
                Price = 349.99m,
                Thumbnail = "https://http2.mlstatic.com/fake/caixa.jpg",
                Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-3",
                CategoryId = CategoriaAudio,
                Shipping = new MlShippingDto { FreeShipping = false }
            },
            "MLB-FAKE-4" => new MlItemDetailDto
            {
                Id = idMl,
                Title = "Cabo USB-C 2m (fake)",
                Price = 29.90m,
                Thumbnail = "https://http2.mlstatic.com/fake/cabo.jpg",
                Permalink = "https://www.mercadolivre.com.br/fake/MLB-FAKE-4",
                CategoryId = CategoriaAudio,
                Shipping = new MlShippingDto { FreeShipping = false }
            },
            _ => null
        };

        return Task.FromResult(item);
    }

    public Task<MlCategoryDto?> ObterCategoriaAsync(string categoriaId, CancellationToken ct = default)
    {
        MlCategoryDto? categoria = CategoriasPorId.TryGetValue(categoriaId, out var nome)
            ? new MlCategoryDto { Id = categoriaId, Name = nome }
            : null;

        return Task.FromResult(categoria);
    }
}
