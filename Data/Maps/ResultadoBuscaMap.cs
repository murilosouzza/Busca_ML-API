using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuscaML.Api.Data.Maps;

public class ResultadoBuscaMap : IEntityTypeConfiguration<ResultadoBusca>
{
    public void Configure(EntityTypeBuilder<ResultadoBusca> builder)
    {
        builder.ToTable("resultados_busca");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PrecoEncontrado)
            .HasColumnType("numeric(18,2)");

        builder.HasOne(r => r.Busca)
            .WithMany(b => b.Resultados)
            .HasForeignKey(r => r.BuscaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Produto)
            .WithMany()
            .HasForeignKey(r => r.ProdutoIdMl)
            .HasPrincipalKey(p => p.IdMl)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.BuscaId);
    }
}
