using System.Text.Json;
using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuscaML.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Busca> Buscas => Set<Busca>();
    public DbSet<ResultadoBusca> ResultadosBusca => Set<ResultadoBusca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<HighlightCache> HighlightsCache => Set<HighlightCache>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // SQLite não faz ORDER BY em DateTimeOffset. Este converter grava um long
        // que preserva ordenação e offset, então as queries do BuscaService funcionam.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EF Core não mapeia JsonDocument nativamente: guardamos o texto cru e reidratamos.
        var jsonDocumentConverter = new ValueConverter<JsonDocument, string>(
            doc => doc.RootElement.GetRawText(),
            text => JsonDocument.Parse(text, default));

        var jsonDocumentComparer = new ValueComparer<JsonDocument>(
            (a, b) => a!.RootElement.GetRawText() == b!.RootElement.GetRawText(),
            doc => doc.RootElement.GetRawText().GetHashCode(),
            doc => JsonDocument.Parse(doc.RootElement.GetRawText(), default));

        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("categorias");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("produtos");
            e.HasKey(p => p.IdMl);
            e.Property(p => p.IdMl).ValueGeneratedNever();
            e.Property(p => p.Preco).HasColumnType("decimal(18,2)");
            e.Property(p => p.PayloadBruto)
                .HasConversion(jsonDocumentConverter)
                .Metadata.SetValueComparer(jsonDocumentComparer);

            e.HasOne(p => p.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(p => p.MaisVendido);
        });

        modelBuilder.Entity<Busca>(e =>
        {
            e.ToTable("buscas");
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.TermoPesquisado, b.DataHora });
        });

        modelBuilder.Entity<ResultadoBusca>(e =>
        {
            e.ToTable("resultados_busca");
            e.HasKey(r => r.Id);
            e.Property(r => r.PrecoEncontrado).HasColumnType("decimal(18,2)");

            e.HasOne(r => r.Busca)
                .WithMany(b => b.Resultados)
                .HasForeignKey(r => r.BuscaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Produto)
                .WithMany()
                .HasForeignKey(r => r.ProdutoIdMl)
                .HasPrincipalKey(p => p.IdMl)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.BuscaId);
        });

        modelBuilder.Entity<HighlightCache>(e =>
        {
            e.ToTable("highlights_cache");
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.CategoriaId).IsUnique();
            e.Property(h => h.PayloadBruto)
                .HasConversion(jsonDocumentConverter)
                .Metadata.SetValueComparer(jsonDocumentComparer);
        });
    }
}
