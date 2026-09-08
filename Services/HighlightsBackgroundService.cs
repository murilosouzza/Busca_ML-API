using System.Text.Json;
using BuscaML.Api.Data;
using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Services;

public class HighlightsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HighlightsBackgroundService> _logger;
    private readonly TimeSpan _intervalo;

    public HighlightsBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<HighlightsBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var horas = config.GetValue<double?>("Cache:IntervaloAtualizacaoHighlightsHoras") ?? 1.0;
        _intervalo = TimeSpan.FromHours(horas);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HighlightsBackgroundService iniciado. Intervalo: {Intervalo}.", _intervalo);

        using var timer = new PeriodicTimer(_intervalo);

        do
        {
            await AtualizarHighlightsDeTodasCategoriasAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task AtualizarHighlightsDeTodasCategoriasAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ml = scope.ServiceProvider.GetRequiredService<IMercadoLivreClient>();

            var categorias = await db.Produtos
                .Where(p => p.CategoriaId != null)
                .Select(p => p.CategoriaId!)
                .Distinct()
                .ToListAsync(ct);

            _logger.LogInformation("Atualizando highlights de {Qtd} categoria(s).", categorias.Count);

            foreach (var categoriaId in categorias)
            {
                try
                {
                    var highlights = await ml.ObterHighlightsAsync(categoriaId, ct);
                    var json = JsonSerializer.SerializeToDocument(highlights);

                    var cache = await db.HighlightsCache.FirstOrDefaultAsync(h => h.CategoriaId == categoriaId, ct);
                    if (cache is null)
                    {
                        db.HighlightsCache.Add(new HighlightCache
                        {
                            CategoriaId = categoriaId,
                            PayloadBruto = json,
                            AtualizadoEm = DateTimeOffset.UtcNow
                        });
                    }
                    else
                    {
                        cache.PayloadBruto = json;
                        cache.AtualizadoEm = DateTimeOffset.UtcNow;
                    }

                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao atualizar highlights da categoria {CategoriaId}.", categoriaId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no HighlightsBackgroundService.");
        }
    }
}
