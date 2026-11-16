using BuscaML.Api.Repositories.Interfaces;
using BuscaML.Api.Services.Interfaces;

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

        double horas = config.GetValue<double?>("Cache:IntervaloAtualizacaoHighlightsHoras") ?? 1.0;
        _intervalo = TimeSpan.FromHours(horas);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HighlightsBackgroundService iniciado. Intervalo: {Intervalo}.", _intervalo);

        using PeriodicTimer timer = new PeriodicTimer(_intervalo);

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
            using IServiceScope scope = _scopeFactory.CreateScope();
            IProdutoRepository produtoRepository = scope.ServiceProvider.GetRequiredService<IProdutoRepository>();
            IHighlightService highlightService = scope.ServiceProvider.GetRequiredService<IHighlightService>();

            List<string> categorias = await produtoRepository.ListarCategoriaIdsAsync(ct);

            _logger.LogInformation("Atualizando highlights de {Qtd} categoria(s).", categorias.Count);

            foreach (string categoriaId in categorias)
            {
                try
                {
                    await highlightService.AtualizarAsync(categoriaId, ct);
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
