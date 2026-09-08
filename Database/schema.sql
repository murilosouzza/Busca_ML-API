CREATE TABLE "buscas" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_buscas" PRIMARY KEY AUTOINCREMENT,
    "TermoPesquisado" TEXT NOT NULL,
    "DataHora" INTEGER NOT NULL
);


CREATE TABLE "categorias" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_categorias" PRIMARY KEY,
    "NomeMl" TEXT NOT NULL
);


CREATE TABLE "highlights_cache" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_highlights_cache" PRIMARY KEY AUTOINCREMENT,
    "CategoriaId" TEXT NOT NULL,
    "PayloadBruto" TEXT NOT NULL,
    "AtualizadoEm" INTEGER NOT NULL
);


CREATE TABLE "produtos" (
    "IdMl" TEXT NOT NULL CONSTRAINT "PK_produtos" PRIMARY KEY,
    "Titulo" TEXT NOT NULL,
    "Preco" decimal(18,2) NOT NULL,
    "CategoriaId" TEXT NULL,
    "PosicaoRanking" INTEGER NULL,
    "AtualizadoEm" INTEGER NOT NULL,
    "FreteGratis" INTEGER NOT NULL,
    "ImagemUrl" TEXT NULL,
    "LinkMl" TEXT NULL,
    "MaisVendido" INTEGER NOT NULL,
    "PayloadBruto" TEXT NOT NULL,
    CONSTRAINT "FK_produtos_categorias_CategoriaId" FOREIGN KEY ("CategoriaId") REFERENCES "categorias" ("Id") ON DELETE SET NULL
);


CREATE TABLE "resultados_busca" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_resultados_busca" PRIMARY KEY AUTOINCREMENT,
    "BuscaId" INTEGER NOT NULL,
    "ProdutoIdMl" TEXT NOT NULL,
    "PrecoEncontrado" decimal(18,2) NOT NULL,
    "PosicaoRanking" INTEGER NULL,
    "RegistradoEm" INTEGER NOT NULL,
    CONSTRAINT "FK_resultados_busca_buscas_BuscaId" FOREIGN KEY ("BuscaId") REFERENCES "buscas" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_resultados_busca_produtos_ProdutoIdMl" FOREIGN KEY ("ProdutoIdMl") REFERENCES "produtos" ("IdMl") ON DELETE CASCADE
);


CREATE INDEX "IX_buscas_TermoPesquisado_DataHora" ON "buscas" ("TermoPesquisado", "DataHora");


CREATE UNIQUE INDEX "IX_highlights_cache_CategoriaId" ON "highlights_cache" ("CategoriaId");


CREATE INDEX "IX_produtos_CategoriaId" ON "produtos" ("CategoriaId");


CREATE INDEX "IX_produtos_MaisVendido" ON "produtos" ("MaisVendido");


CREATE INDEX "IX_resultados_busca_BuscaId" ON "resultados_busca" ("BuscaId");


CREATE INDEX "IX_resultados_busca_ProdutoIdMl" ON "resultados_busca" ("ProdutoIdMl");


