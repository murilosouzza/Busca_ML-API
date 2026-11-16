using BuscaML.Api.Data;
using BuscaML.Api.Models;
using BuscaML.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly AppDbContext _context;

    public ProdutoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Produto?> BuscarPorIdMlAsync(string idMl, CancellationToken ct = default)
    {
        return await _context.Produtos
            .FirstOrDefaultAsync(p => p.IdMl == idMl, ct);
    }

    public async Task<Produto?> BuscarPorIdMlComCategoriaAsync(string idMl, CancellationToken ct = default)
    {
        return await _context.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.IdMl == idMl, ct);
    }

    public async Task<List<Produto>> ListarDestaquesAsync(int quantidade, CancellationToken ct = default)
    {
        return await _context.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.MaisVendido)
            .OrderBy(p => p.PosicaoRanking ?? int.MaxValue)
            .ThenByDescending(p => p.AtualizadoEm)
            .Take(quantidade)
            .ToListAsync(ct);
    }

    public async Task<List<string>> ListarCategoriaIdsAsync(CancellationToken ct = default)
    {
        return await _context.Produtos
            .Where(p => p.CategoriaId != null)
            .Select(p => p.CategoriaId!)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<bool> ExisteAlgumAsync(CancellationToken ct = default)
    {
        return await _context.Produtos.AnyAsync(ct);
    }

    public async Task AdicionarAsync(Produto produto, CancellationToken ct = default)
    {
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Produto produto, CancellationToken ct = default)
    {
        _context.Produtos.Update(produto);
        await _context.SaveChangesAsync(ct);
    }
}
