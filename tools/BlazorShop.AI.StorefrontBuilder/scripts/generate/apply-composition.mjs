#!/usr/bin/env node
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const target = readArg("--target") ?? "";

const transforms = [
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

  const path = join(projectRoot, relativePath);
  const original = readFileSync(path, "utf8");
  const updated = transform(original);
  if (updated !== original) {
    writeFileSync(path, updated, "utf8");
  }
}

console.log("StorefrontBuilder composition applied shell, home, catalog, product, and fallback page files from generation-plan.yaml.");
console.log("Commerce commands remain bound through Starter slot/action contracts such as cart.add-line.");

function transformLayout(content) {
  if (content.includes("sfb-shell-header")) {
    return content;
  }

  return content
    .replace('<header class="starter-header">', '<header class="starter-header sfb-shell-header">')
    .replace('<nav aria-label="Main navigation">', '<nav class="sfb-main-nav" aria-label="Main navigation">')
    .replace('<a href="/cart" aria-label="Cart">Cart</a>', '<a class="sfb-cart-badge" href="/cart" aria-label="Cart">Cart <span>0</span></a>')
    .replace(
      "</header>",
      '<nav class="sfb-mobile-nav" aria-label="Mobile navigation"><a href="/">Home</a><a href="/cart">Cart</a><a href="/account">Account</a></nav>\n</header>'
    );
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
  return content.replace("<h1>", '<h1 class="sfb-product-page">');
}

function transformGallery(content) {
  return content.replace('class="starter-gallery-placeholder"', 'class="starter-gallery-placeholder sfb-product-gallery"');
}

function transformPurchasePanel(content) {
  if (content.includes('data-action="cart.add-line"')) {
    return content;
  }

  return content
    .replace('<aside class="starter-purchase-panel">', '<aside class="starter-purchase-panel sfb-product-purchase">')
    .replace(
      '<button class="starter-button" type="button" disabled>Add to cart</button>',
      '<label class="sfb-quantity-control">Quantity <input type="number" min="1" value="1" /></label>\n    <button class="starter-button" data-action="cart.add-line" type="button" disabled>Add to cart</button>'
    );
}

function transformFallbackPage(content) {
  return content.replace("<h1>", '<h1 class="sfb-fallback-page">');
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
