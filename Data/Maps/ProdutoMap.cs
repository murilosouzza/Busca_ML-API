using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuscaML.Api.Data.Maps;

public class ProdutoMap : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");

        // A chave é o IdMl (ex.: "MLB1234"), o mesmo código que o Mercado Livre usa.
        builder.HasKey(p => p.IdMl);
        builder.Property(p => p.IdMl).ValueGeneratedNever();

        builder.Property(p => p.Preco)
            .HasColumnType("numeric(18,2)");

        builder.Property(p => p.PayloadBruto)
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(JsonDocumentComparer.Criar());

        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Produtos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.MaisVendido);
    }
}
