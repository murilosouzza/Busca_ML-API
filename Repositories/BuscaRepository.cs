using BuscaML.Api.Data;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Repositories;

public class BuscaRepository : IBuscaRepository
{
    private readonly AppDbContext _context;

    public BuscaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Busca?> BuscarUltimaPorTermoAsync(string termoNormalizado, CancellationToken ct = default)
    {
        return await _context.Buscas
            .Where(b => b.TermoPesquisado == termoNormalizado)
            .OrderByDescending(b => b.DataHora)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ResultadoBusca>> ListarResultadosAsync(int buscaId, CancellationToken ct = default)
    {
        return await _context.ResultadosBusca
            .Where(r => r.BuscaId == buscaId)
            .Include(r => r.Produto).ThenInclude(p => p!.Categoria)
            .OrderBy(r => r.Id) // mesma ordem em que os resultados foram salvos
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(Busca busca, CancellationToken ct = default)
    {
        _context.Buscas.Add(busca);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AdicionarResultadosAsync(List<ResultadoBusca> resultados, CancellationToken ct = default)
    {
        _context.ResultadosBusca.AddRange(resultados);
        await _context.SaveChangesAsync(ct);
    }
}
