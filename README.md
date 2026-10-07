# BuscaML — API

Backend do BuscaML: busca produtos e os mais vendidos do Mercado Livre, guarda tudo em cache num banco SQLite e entrega para o [front](https://github.com/murilosouzza/BuscaML-Front).

Feito em C# com ASP.NET Core (.NET 10) e Entity Framework Core.

## Como rodar

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/murilosouzza/Busca_ML-API.git
cd Busca_ML-API
dotnet run
```

A API sobe em `http://localhost:5102`. O banco (`buscaml.db`) é criado sozinho na primeira execução.

Sem nenhuma configuração, o projeto roda com a **API fake**: quatro produtos fixos, sem depender de internet nem de credencial. Serve para desenvolver o front e testar o fluxo.

## Como usar a API real do Mercado Livre

A API oficial exige as credenciais da aplicação cadastrada no [DevCenter do Mercado Livre](https://developers.mercadolivre.com.br/devcenter). Elas **não ficam no repositório**: cada pessoa guarda na própria máquina, nos segredos do usuário do .NET.

Peça o `ClientId` e o `ClientSecret` no grupo e escolha um dos dois jeitos.

**Pelo Visual Studio:** botão direito no projeto > *Gerenciar Segredos do Usuário*, e cole:

```json
{
  "MercadoLivre": {
    "ClientId": "COLE_O_CLIENT_ID",
    "ClientSecret": "COLE_O_CLIENT_SECRET",
    "UseFake": false
  }
}
```

**Pelo terminal**, na pasta do projeto:

```bash
dotnet user-secrets set "MercadoLivre:ClientId" "COLE_O_CLIENT_ID"
dotnet user-secrets set "MercadoLivre:ClientSecret" "COLE_O_CLIENT_SECRET"
dotnet user-secrets set "MercadoLivre:UseFake" "false"
```

Depois apague o `buscaml.db` (para limpar os produtos fake do cache) e rode de novo. No log deve aparecer `Novo token do Mercado Livre obtido`.

Para voltar à API fake, troque `UseFake` para `true`.

> Nunca coloque o `ClientSecret` no `appsettings.json` nem em nenhum arquivo do repositório.

## Endpoints

| Método | Rota | O que devolve |
|---|---|---|
| GET | `/api/buscar?termo=notebook` | Produtos que batem com o termo, completados com os mais vendidos da categoria |
| GET | `/api/categorias/{id}/mais-vendidos` | Ranking de mais vendidos de uma categoria do Mercado Livre (ex.: `MLB1648`) |
| GET | `/api/produtos/destaques?quantidade=20` | Mais vendidos já salvos no banco (usado na home) |
| GET | `/api/produtos/{id}` | Um produto pelo id (ex.: `MLB70040749`) |

Todos devolvem produtos no mesmo formato, que é o contrato com o front:

```json
{
  "termo": "notebook",
  "total_resultados": 20,
  "resultados": [
    {
      "id": "MLB70040749",
      "titulo": "Notebook ...",
      "preco": 4349.33,
      "categoria": "Notebooks",
      "mais_vendido": true,
      "frete_gratis": true,
      "imagem_url": "https://http2.mlstatic.com/...",
      "link_ml": "https://www.mercadolivre.com.br/p/MLB70040749"
    }
  ]
}
```

## Como funciona

**De onde vêm os dados.** O endpoint clássico de busca do Mercado Livre (`/sites/MLB/search`) responde 403 para a nossa aplicação. Por isso a busca é montada com os endpoints de catálogo, que funcionam com o token `client_credentials`:

| Endpoint do Mercado Livre | Para que serve |
|---|---|
| `/oauth/token` | Gera o token de acesso (vale 6 horas e é renovado sozinho) |
| `/products/search` | Acha produtos de catálogo por palavra-chave, sem preço |
| `/products/{id}` | Nome e fotos do produto |
| `/products/{id}/items` | Anúncios do produto: preço, frete e categoria |
| `/highlights/MLB/category/{id}` | Ranking de mais vendidos da categoria |
| `/sites/MLB/domain_discovery/search` | Descobre a categoria mais provável de um termo |
| `/categories/{id}` | Nome da categoria |

Produto de catálogo sem anúncio ativo não tem preço, então é descartado.

**Cache.** Cada busca é salva no SQLite. Repetir o mesmo termo (ou a mesma categoria) dentro de 30 minutos responde direto do banco, sem chamar o Mercado Livre. O tempo é configurável em `Cache:TtlMinutosBusca`.

**Resiliência.** Se o Mercado Livre falhar ou demorar, a API devolve a última busca salva para aquele termo, mesmo vencida. Só responde 404 se nunca houve resultado salvo.

**Job assíncrono.** O `HighlightsBackgroundService` roda em segundo plano desde a subida da aplicação e, de hora em hora, atualiza o ranking de mais vendidos de todas as categorias já vistas. Não trava nenhuma requisição.

**API fake x real.** As duas implementam a mesma interface (`IMercadoLivreClient`). O `Program.cs` escolhe qual usar pela configuração `MercadoLivre:UseFake`, e o resto do código não sabe a diferença.

## Estrutura

```
Program.cs                              monta tudo: banco, clients, CORS, carga inicial
Controllers/
  BuscaController.cs                    GET /api/buscar
  CategoriasController.cs               GET /api/categorias/{id}/mais-vendidos
  ProdutosController.cs                 GET /api/produtos/destaques e /api/produtos/{id}
Services/
  IMercadoLivreClient.cs                contrato de acesso ao Mercado Livre
  MercadoLivreClient.cs                 implementação real (API oficial)
  FakeMercadoLivreClient.cs             implementação fake (dados fixos)
  MercadoLivreAuthHandler.cs            gera o token e coloca em cada chamada
  BuscaService.cs                       regra de negócio: cache, ranking, fallback
  HighlightsBackgroundService.cs        job que atualiza os mais vendidos
DTOs/
  BuscaResponseDto.cs                   formato de resposta para o front
  MercadoLivreDtos.cs                   formato interno dos dados do Mercado Livre
Models/ e Data/AppDbContext.cs          entidades e mapeamento do SQLite
docs/                                   documentos do projeto
```

## Problemas comuns

- **A primeira subida demora.** Com o banco vazio, a API faz três buscas iniciais (celular, fone, notebook) para a home ter o que mostrar. Com a API real são mais de cem chamadas ao Mercado Livre, o que leva alguns segundos.
- **Continuam aparecendo produtos "(fake)".** Eles estão no cache. Pare a API, apague `buscaml.db`, `buscaml.db-shm` e `buscaml.db-wal` e rode de novo.
- **Muitos 404 no log.** É esperado: são produtos de catálogo sem anúncio ativo, que a API descarta.
- **`403` em `/sites/MLB/search`.** Esse endpoint é bloqueado pelo Mercado Livre e não é mais usado. Se aparecer, o `MercadoLivreClient.cs` está desatualizado: dê `git pull`.
- **Falha ao obter token.** Confira o `ClientId` e o `ClientSecret` nos segredos do usuário.
