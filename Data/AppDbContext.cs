using BuscaML.Api.Data.Maps;
using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuscaML.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Busca> Buscas => Set<Busca>();
    public DbSet<ResultadoBusca> ResultadosBusca => Set<ResultadoBusca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<HighlightCache> HighlightsCache => Set<HighlightCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CategoriaMap());
        modelBuilder.ApplyConfiguration(new ProdutoMap());
        modelBuilder.ApplyConfiguration(new BuscaMap());
        modelBuilder.ApplyConfiguration(new ResultadoBuscaMap());
        modelBuilder.ApplyConfiguration(new HighlightCacheMap());

        base.OnModelCreating(modelBuilder);
    }
}
