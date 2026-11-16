using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuscaML.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buscas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TermoPesquisado = table.Column<string>(type: "text", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buscas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    NomeMl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "highlights_cache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoriaId = table.Column<string>(type: "text", nullable: false),
                    PayloadBruto = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_highlights_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    IdMl = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CategoriaId = table.Column<string>(type: "text", nullable: true),
                    PosicaoRanking = table.Column<int>(type: "integer", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FreteGratis = table.Column<bool>(type: "boolean", nullable: false),
                    ImagemUrl = table.Column<string>(type: "text", nullable: true),
                    LinkMl = table.Column<string>(type: "text", nullable: true),
                    MaisVendido = table.Column<bool>(type: "boolean", nullable: false),
                    PayloadBruto = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos", x => x.IdMl);
                    table.ForeignKey(
                        name: "FK_produtos_categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "resultados_busca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuscaId = table.Column<int>(type: "integer", nullable: false),
                    ProdutoIdMl = table.Column<string>(type: "text", nullable: false),
                    PrecoEncontrado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PosicaoRanking = table.Column<int>(type: "integer", nullable: true),
                    RegistradoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultados_busca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resultados_busca_buscas_BuscaId",
                        column: x => x.BuscaId,
                        principalTable: "buscas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resultados_busca_produtos_ProdutoIdMl",
                        column: x => x.ProdutoIdMl,
                        principalTable: "produtos",
                        principalColumn: "IdMl",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buscas_TermoPesquisado_DataHora",
                table: "buscas",
                columns: new[] { "TermoPesquisado", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_highlights_cache_CategoriaId",
                table: "highlights_cache",
                column: "CategoriaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_produtos_CategoriaId",
                table: "produtos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_MaisVendido",
                table: "produtos",
                column: "MaisVendido");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_busca_BuscaId",
                table: "resultados_busca",
                column: "BuscaId");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_busca_ProdutoIdMl",
                table: "resultados_busca",
                column: "ProdutoIdMl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "highlights_cache");

            migrationBuilder.DropTable(
                name: "resultados_busca");

            migrationBuilder.DropTable(
                name: "buscas");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}
