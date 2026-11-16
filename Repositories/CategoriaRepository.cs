using BuscaML.Api.Data;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly AppDbContext _context;

    public CategoriaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteAsync(string id, CancellationToken ct = default)
    {
        return await _context.Categorias.AnyAsync(c => c.Id == id, ct);
    }

    public async Task<Dictionary<string, string>> ListarNomesPorIdAsync(CancellationToken ct = default)
    {
        return await _context.Categorias.ToDictionaryAsync(c => c.Id, c => c.NomeMl, ct);
    }

    public async Task AdicionarAsync(Categoria categoria, CancellationToken ct = default)
    {
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync(ct);
    }
}
