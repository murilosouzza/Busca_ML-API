using BuscaML.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuscaML.Api.Data.Maps;

public class BuscaMap : IEntityTypeConfiguration<Busca>
{
    public void Configure(EntityTypeBuilder<Busca> builder)
    {
        builder.ToTable("buscas");

        builder.HasKey(b => b.Id);

        // Acelera a consulta do cache: "última busca deste termo".
        builder.HasIndex(b => new { b.TermoPesquisado, b.DataHora });
    }
}
