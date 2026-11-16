using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuscaML.Api.Data.Maps;

public class HighlightCacheMap : IEntityTypeConfiguration<HighlightCache>
{
    public void Configure(EntityTypeBuilder<HighlightCache> builder)
    {
        builder.ToTable("highlights_cache");

        builder.HasKey(h => h.Id);

        // Uma linha de cache por categoria.
        builder.HasIndex(h => h.CategoriaId)
            .IsUnique();

        builder.Property(h => h.PayloadBruto)
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(JsonDocumentComparer.Criar());
    }
}
