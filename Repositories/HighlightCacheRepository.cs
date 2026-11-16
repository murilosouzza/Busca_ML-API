using BuscaML.Api.Data;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Repositories;

public class HighlightCacheRepository : IHighlightCacheRepository
{
    private readonly AppDbContext _context;

    public HighlightCacheRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HighlightCache?> BuscarPorCategoriaAsync(string categoriaId, CancellationToken ct = default)
    {
        return await _context.HighlightsCache
            .FirstOrDefaultAsync(h => h.CategoriaId == categoriaId, ct);
    }

    public async Task AdicionarAsync(HighlightCache cache, CancellationToken ct = default)
    {
        _context.HighlightsCache.Add(cache);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(HighlightCache cache, CancellationToken ct = default)
    {
        _context.HighlightsCache.Update(cache);
        await _context.SaveChangesAsync(ct);
    }
}
