#!/usr/bin/env node
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { basename, join, resolve } from "node:path";

const workspaceRoot = resolve(readArg("--workspace-root") ?? readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const serverRoot = resolveServerRoot(workspaceRoot);
const target = readArg("--target") ?? "";

const transforms = [
  ["Components/Layout/ApplicationHead.razor", transformApplicationHead],
  ["Components/Layout/MainLayout.razor", transformLayout],
  ["Pages/Ssr/Home/HomePage.razor", transformHome],
  ["Pages/Hybrid/Catalog/CategoryPage.razor", transformCategory],
  ["Components/Catalog/ProductSummaryCard.razor", transformProductCard],
  ["Pages/Hybrid/Catalog/ProductPage.razor", transformProductPage],
  ["Components/Catalog/ProductGalleryPlaceholder.razor", transformGallery],
  ["Components/Catalog/PurchasePanelPlaceholder.razor", transformPurchasePanel],
  ["Pages/Hybrid/Commerce/CartPage.razor", transformFallbackPage],
  ["Pages/Hybrid/Commerce/CheckoutPage.razor", transformFallbackPage],
  ["Pages/WasmHost/Account/AccountHostPage.razor", transformFallbackPage],
];

for (const [relativePath, transform] of transforms) {
  if (target && !relativePath.toLowerCase().includes(target.toLowerCase())) {
    continue;
  }

  const path = join(serverRoot, relativePath);
  const original = readFileSync(path, "utf8");
  const updated = transform(original);
  if (updated !== original) {
    writeFileSync(path, updated, "utf8");
  }
}

console.log("StorefrontBuilder composition applied deterministic shell, home, catalog, product, and fallback page transforms from Starter.");
console.log("Commerce commands remain bound through Presentation product purchase descriptors such as cart.add-line.");

function transformLayout(content) {
    if (content.includes("sfb-shell-header")) {
        return content;
    }

  return content
    .replace('<header class="starter-header">', '<header class="starter-header sfb-shell-header">')
    .replace('<nav aria-label="Main navigation">', '<nav class="sfb-main-nav" aria-label="Main navigation">')
    .replace(
      '<a href="@Context.Links.Search.Href">@Context.Links.Search.Label</a>',
      '<a href="@Context.Links.Search.Href">@Context.Links.Search.Label</a>\n        @foreach (var category in Context.Search.Categories)\n        {\n            <a href="@Context.Links.Category(category.Href)">@category.Label</a>\n        }'
    )
    .replace('<a href="@Context.Links.Cart.Href" aria-label="Cart">@Context.Links.Cart.Label <span data-storefront-cart-badge hidden>0</span></a>', '<a class="sfb-cart-badge" href="@Context.Links.Cart.Href" aria-label="Cart">@Context.Links.Cart.Label <span data-storefront-cart-badge hidden>0</span></a>')
    .replace(
      "</header>",
      '<nav class="sfb-mobile-nav" aria-label="Mobile navigation"><a href="@Context.Links.Home.Href">@Context.Links.Home.Label</a><a href="@Context.Links.Cart.Href">@Context.Links.Cart.Label</a><a href="@Context.Links.AccountRoot.Href">@Context.Links.AccountRoot.Label</a></nav>\n</header>'
    );
}

function transformApplicationHead(content) {
  let updated = content.replace(/\s*<script src="js\/storefront-builder\.functional\.js" defer><\/script>\r?\n?/g, "\n");

  if (!updated.includes("css/storefront-builder.generated.css")) {
    updated = updated.replace(
      '<link rel="stylesheet" href="css/starter.css" />',
      '<link rel="stylesheet" href="css/starter.css" />\n    <link rel="stylesheet" href="css/storefront-builder.generated.css" />'
    );
  }

  return updated;
}

function transformHome(content) {
  return content
    .replace("<h1>", '<h1 class="sfb-hero">')
    .replace('<section class="starter-section" aria-labelledby="featured-products-title"', '<section class="starter-section sfb-featured-grid" aria-labelledby="featured-products-title"');
}

function transformCategory(content) {
  if (content.includes("sfb-catalog-toolbar")) {
    return content;
  }

  return content.replace(
    "<PlaceholderState",
    '<section class="sfb-catalog-toolbar" aria-label="Catalog controls"><label>Sort <select><option>Featured</option></select></label></section>\n<PlaceholderState'
  );
}

function transformProductCard(content) {
  return content.replace('class="starter-product-card"', 'class="starter-product-card sfb-product-card"');
}

function transformProductPage(content) {
  return content
    .replace("<h1>", '<h1 class="sfb-product-page">');
}

function transformGallery(content) {
  return content.replace('class="starter-gallery-placeholder"', 'class="starter-gallery-placeholder sfb-product-gallery"');
}

function transformPurchasePanel(content) {
  if (content.includes("sfb-product-purchase")) {
    return content;
  }

  return content
    .replace('class="starter-purchase-panel"', 'class="starter-purchase-panel sfb-product-purchase"')
    .replace('class="starter-quantity-control"', 'class="starter-quantity-control sfb-quantity-control"');
}

function transformFallbackPage(content) {
  return content.replace("<h1>", '<h1 class="sfb-fallback-page">');
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function resolveServerRoot(root) {
  const projectName = basename(root);
  const starterFirstRoot = join(root, projectName);
  return existsSync(join(starterFirstRoot, `${projectName}.csproj`))
    ? starterFirstRoot
    : root;
}
