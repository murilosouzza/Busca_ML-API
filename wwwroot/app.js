
// ÍCONES SVG REUTILIZADOS NOS CARDS DOS PRODUTOS
const ICONS = {

  verified: `
    <svg viewBox="0 0 24 24" fill="currentColor">

      <path d="M12 2l2.4 2.2 3.2-.5 1 3.1 3 1.3-1 3.2
        1 3.2-3 1.3-1 3.1-3.2-.5L12 22l-2.4-2.2-3.2.5
        -1-3.1-3-1.3 1-3.2-1-3.2 3-1.3 1-3.1 3.2.5L12 2z"/>

      <path
        d="M9 12l2 2 4-4"
        stroke="#fff"
        stroke-width="2"
        fill="none"
        stroke-linecap="round"
        stroke-linejoin="round"
      />

    </svg>
  `,

  star: `
    <svg viewBox="0 0 24 24" fill="currentColor">

      <path d="M12 2l2.9 6.3 6.9.7-5.2 4.7
        1.5 6.8-6.1-3.6-6.1 3.6
        1.5-6.8-5.2-4.7 6.9-.7L12 2z"/>

    </svg>
  `,

  refresh: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    >

      <polyline points="1 4 1 10 7 10"/>
      <polyline points="23 20 23 14 17 14"/>

      <path d="M3.5 9a9 9 0 0114.6-3.4L23 10"/>
      <path d="M1 14l4.9 4.4A9 9 0 0020.5 15"/>

    </svg>
  `,

  bolt: `
    <svg viewBox="0 0 24 24" fill="currentColor">

      <path d="M13 2L3 14h7l-1 8 11-13h-7l0-7z"/>

    </svg>
  `,

  chevronRight: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2.4"
      stroke-linecap="round"
      stroke-linejoin="round"
    >

      <polyline points="9 6 15 12 9 18"/>

    </svg>
  `
};

// CATEGORIAS DA HOME
const CATEGORIES = [

  {
    name: "Games",
    icon: '<img src="icones/game.png" alt="">'
  },

  {
    name: "Eletrodomésticos",
    icon: '<img src="icones/eletrodomestico.png" alt="">'
  },

  {
    name: "Ferramentas",
    icon: '<img src="icones/caixa-de-ferramentas.png" alt="">'
  },

  {
    name: "Beleza e Cuidado Pessoal",
    icon: '<img src="icones/perfume.png" alt="">'
  },

  {
    name: "Celulares e Telefones",
    icon: '<img src="icones/smartphone.png" alt="">'
  },

  {
    name: "Informática",
    icon: '<img src="icones/informatica.png" alt="">'
  },

  {
    name: "Eletrônicos, Áudio e Vídeo",
    icon: '<img src="icones/eletronicos.png" alt="">'
  },

  {
    name: "Esportes e Fitness",
    icon: '<img src="icones/esporte.png" alt="">'
  },

  {
    name: "Calçados, Roupas e Bolsas",
    icon: '<img src="icones/roupa.png" alt="">'
  },

  {
    name: "Acessórios para Veículos",
    icon: '<img src="icones/veiculo.png" alt="">'
  }

];

// CONEXÃO COM A API
// Quando o front é servido pelo próprio back (porta 5102), usa caminho relativo.
// Fora disso (Live Server, arquivo aberto direto), aponta pro back local.
const API_BASE =
  (location.port === "5102")
    ? ""
    : "http://localhost:5102";

// Traduz o produto do formato do back (pt/snake_case) pro formato que o card espera.
function mapProduto(r) {

  return {
    id: r.id,
    title: r.titulo,
    price: r.preco,
    image: r.imagem_url,
    productUrl: r.link_ml,
    freeShipping: r.frete_gratis,
    bestSeller: r.mais_vendido,
    category: r.categoria
  };
}

async function fetchProducts(query = "") {

  const termo = (query || "").trim();

  // Sem termo -> destaques (mais vendidos). Com termo -> busca.
  const url = termo
    ? `${API_BASE}/api/buscar?termo=${encodeURIComponent(termo)}`
    : `${API_BASE}/api/produtos/destaques?quantidade=20`;

  try {

    const resp = await fetch(url);

    // 404 = nada encontrado e sem cache; trata como lista vazia.
    if (resp.status === 404) {
      return [];
    }

    if (!resp.ok) {
      console.error("Falha na API:", resp.status);
      return [];
    }

    const data = await resp.json();

    return (data.resultados || []).map(mapProduto);

  } catch (err) {

    console.error("Erro ao chamar a API (o back está rodando?):", err);
    return [];
  }
}

// FORMATAÇÃO DE PREÇOS
function formatPrice(value) {

  if (
    value === null ||
    value === undefined
  ) {

    return "";
  }

  return "R$ " +
    value.toLocaleString(
      "pt-BR",
      {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      }
    );
}

// CORES PARA REPRESENTAR VARIAÇÕES DE PRODUTOS
const PALETTE_GUESS = [

  "#1c2b52",
  "#ffffff",
  "#c9a13b",
  "#7a1f1f",
  "#2e5c3e",
  "#444444",
  "#8a4b9e"

];

// CARD DO PRODUTO
function productCardHTML(p) {

  const hasDiscount =
    p.oldPrice &&
    p.discountPercent;

  const hasInstallments =
    !!p.installments;

  const colorCount =
    p.colors || 0;

  const dotsToShow =
    Math.min(colorCount, 3);

// BOLINHAS DE CORES
  const colorDotsHTML =
    colorCount > 0
      ? `
        <div class="color-dots">

          ${
            Array.from(
              { length: dotsToShow }
            )
            .map(
              (_, i) => `
                <span
                  class="color-dot"
                  style="
                    background:
                    ${PALETTE_GUESS[
                      i % PALETTE_GUESS.length
                    ]}
                  "
                ></span>
              `
            )
            .join("")
          }


          ${
            colorCount > dotsToShow

              ? `
                <span class="color-dot-count">
                  +${colorCount - dotsToShow}
                </span>
              `

              : ""
          }

        </div>
      `

      : "";

// NOTA DO PRODUTO
  const ratingHTML =
    p.rating

      ? `
        <div class="product-rating">

          ${ICONS.star}

          <span>
            ${p.rating.toFixed(1)}
          </span>

        </div>
      `

      : "";

// PREÇO ANTIGO
  const oldPriceHTML =
    hasDiscount
      ? `
        <p class="price-old">
          ${formatPrice(p.oldPrice)}
        </p>
      `

      : "";

// DESCONTO
  const discountBadgeHTML =
    hasDiscount

      ? `
        <span class="price-discount">
          ${p.discountPercent}% OFF
        </span>

        <span class="price-pix">
          no Pix
        </span>
      `

      : "";

// PARCELAMENTO
  const installmentsHTML =
    hasInstallments
      ? `
        <p class="installments">

          ou

          <strong>
            ${formatPrice(p.price)}
          </strong>

          em

          <span class="in-green">

            ${p.installments.count}x

            ${formatPrice(
              p.installments.value
            )}

            ${
              p.installments.noInterest
                ? " sem juros"
                : ""
            }

          </span>

        </p>
      `

      : "";

// PLANO DE TROCA
  const tradeInHTML =
    p.tradeIn

      ? `
        <p class="trade-in">

          ${ICONS.refresh}

          Poupe com o Plano de troca

        </p>
      `

      : "";

// FRETE GRÁTIS
  const freeShippingHTML =
    p.freeShipping
      ? `
        <p class="free-shipping">

          ${ICONS.bolt}

          Frete grátis

          ${
            p.full
              ? `
                <span class="full-tag">
                  FULL
                </span>
              `
              : ""
          }

        </p>
      `

      : "";

// PRODUTO PATROCINADO
  const adTagHTML =
    p.sponsored

      ? `
        <span class="ad-tag">
          Ad
        </span>
      `

      : "";

// MARCA
  const brandHTML =
    p.brand
      ? `
        <p class="product-brand">

          ${p.brand}

          ${
            p.verifiedBrand
              ? ICONS.verified
              : ""
          }

        </p>
      `

      : "";

// LINK PARA O MERCADO LIVRE
  const ctaHTML = `

    <a
      class="product-cta"
      href="${p.productUrl || "#"}"
      target="_blank"
      rel="noopener noreferrer"
    >
      Clique para ver produto
    </a>

  `;

// ESTRUTURA FINAL DO CARD
  return `

    <article
      class="product-card"
      data-id="${p.id}"
    >

      <div class="product-media">

        <img
          class="product-image"
          src="${p.image}"
          alt="${p.title}"
          loading="lazy"
        >

        <button
          class="media-next"
          aria-label="Próxima imagem"
        >

          ${ICONS.chevronRight}

        </button>

        ${colorDotsHTML}

        ${adTagHTML}

      </div>

      <div class="product-body">

        <h3 class="product-title">
          ${p.title}
        </h3>

        ${brandHTML}

        ${ratingHTML}

        ${oldPriceHTML}

        <div class="price-row">

          <span class="price-new">
            ${formatPrice(p.price)}
          </span>

          ${discountBadgeHTML}

        </div>

        ${installmentsHTML}

        ${tradeInHTML}

        ${freeShippingHTML}

        ${ctaHTML}

      </div>
    </article>

  `;
}

// RENDERIZAÇÃO DOS PRODUTOS
function renderProducts(
  products,
  containerEl
) {

  if (
    !products ||
    products.length === 0
  ) {

    containerEl.innerHTML = `

      <p
        style="
          grid-column:1/-1;
          color:#8a8a8a;
          padding:24px 0;
        "
      >
        Nenhum produto encontrado.
        Tente outra busca ou verifique se a API está no ar.
      </p>

    `;

    return;
  }

  containerEl.innerHTML =
    products
      .map(productCardHTML)
      .join("");
}

// RENDERIZAÇÃO DAS CATEGORIAS
function renderCategories() {

  const row =
    document.getElementById(
      "categoriesRow"
    );


  if (!row) {
    return;
  }

  row.innerHTML =
    CATEGORIES
      .map(
        cat => `

          <button
            class="category-pill"
            data-category="${cat.name}"
            type="button"
          >

            ${cat.icon}

            <span>
              ${cat.name}
            </span>

          </button>

        `
      )
      .join("");
}

// ELEMENTOS DA HOME
const categoriesRow =
  document.getElementById(
    "categoriesRow"
  );

const homeGrid =
  document.getElementById(
    "homeProductGrid"
  );

const homeSearchForm =
  document.getElementById(
    "homeSearchForm"
  );

const homeSearchInput =
  document.getElementById(
    "homeSearchInput"
  );

// BUSCA DA HOME
if (homeSearchForm) {

  homeSearchForm.addEventListener(
    "submit",
    (e) => {

      e.preventDefault();

      const q =
        (
          homeSearchInput.value || ""
        ).trim();

      window.location.href =
        q

          ? `busca.html?q=${encodeURIComponent(q)}`

          : "busca.html";
    }
  );
}

// CATEGORIAS DA HOME
if (categoriesRow) {

  renderCategories();

  categoriesRow.addEventListener(
    "click",
    (e) => {

      const pill =
        e.target.closest(
          ".category-pill"
        );


      if (!pill) {
        return;
      }

      e.preventDefault();

      window.location.href =
        `busca.html?categoria=${
          encodeURIComponent(
            pill.dataset.category
          )
        }`;
    }
  );
}

// SETAS DE NAVEGAÇÃO DAS CATEGORIAS
const categoryPrev =
  document.getElementById(
    "categoryPrev"
  );

const categoryNext =
  document.getElementById(
    "categoryNext"
  );

if (
  categoryPrev &&
  categoriesRow
) {

  categoryPrev.addEventListener(
    "click",
    () => {

      categoriesRow.scrollBy({

        left: -350,

        behavior: "smooth"

      });
    }
  );
}

if (
  categoryNext &&
  categoriesRow
) {

  categoryNext.addEventListener(
    "click",
    () => {

      categoriesRow.scrollBy({

        left: 350,
        behavior: "smooth"

      });
    }
  );
}

// PRODUTOS DA HOME
if (homeGrid) {

  fetchProducts()
    .then(
      produtos => {

        renderProducts(
          produtos,
          homeGrid
        );
      }
    );
}

// ELEMENTOS DA PÁGINA DE RESULTADOS
const searchGrid =
  document.getElementById(
    "searchProductGrid"
  );

const resultsSearchForm =
  document.getElementById(
    "resultsSearchForm"
  );

const resultsSearchInputEl =
  document.getElementById(
    "resultsSearchInput"
  );

// BUSCA DA PÁGINA DE RESULTADOS
if (resultsSearchForm) {

  resultsSearchForm.addEventListener(
    "submit",
    (e) => {

      e.preventDefault();

      const q =
        (
          resultsSearchInputEl.value || ""
        ).trim();

      window.location.href =
        q

          ? `busca.html?q=${encodeURIComponent(q)}`

          : "busca.html";
    }
  );
}

// RESULTADOS DA BUSCA
if (searchGrid) {

  const resultCountEl =
    document.getElementById(
      "resultCount"
    );

  const resultsSearchInput =
    document.getElementById(
      "resultsSearchInput"
    );

// LÊ OS PARÂMETROS DA URL
  const params =
    new URLSearchParams(
      window.location.search
    );

  const query =
    params.get("q") || "";

  const categoria =
    params.get("categoria") || "";

  const termo =
    query || categoria;

// PREENCHE A BUSCA
  if (resultsSearchInput) {

    resultsSearchInput.value =
      query;

  }

// TEXTO INICIAL
  resultCountEl.textContent =
    termo

      ? `Resultados para "${termo}"`
      : "Resultados";

// BUSCA OS PRODUTOS
  fetchProducts(termo)

    .then(
      produtos => {

        resultCountEl.textContent =
          termo

            ? `${produtos.length} resultado${
                produtos.length !== 1
                  ? "s"
                  : ""
              } para "${termo}"`

            : `${produtos.length} resultado${
                produtos.length !== 1
                  ? "s"
                  : ""
              }`;

        renderProducts(
          produtos,
          searchGrid
        );
      }
    );
}