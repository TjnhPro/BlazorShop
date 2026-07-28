#!/usr/bin/env node
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const target = readArg("--target") ?? "";

const transforms = [
  ["Components/Layout/ApplicationHead.razor", transformApplicationHead],
  ["Components/Layout/MainLayout.razor", transformLayout],
  ["Pages/Ssr/Home/HomePage.razor", transformHome],
  ["Pages/Hybrid/Catalog/CategoryPage.razor", transformCategory],
  ["Components/Catalog/ProductSummaryCard.razor", transformProductCard],
  ["Pages/Hybrid/Catalog/ProductPage.razor", transformProductPage],
  ["Components/Catalog/ProductDetailShell.razor", transformProductDetailShell],
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
console.log("Commerce commands remain bound through Presentation product purchase descriptors such as cart.add-line.");

function transformLayout(content) {
    if (content.includes("sfb-shell-header")) {
        return content;
    }

  return content
    .replace('<header class="starter-header">', '<header class="starter-header sfb-shell-header">')
    .replace('<nav aria-label="Main navigation">', '<nav class="sfb-main-nav" aria-label="Main navigation">')
    .replace(
      '<a href="@Context.Links.TodaysDeals.Href">Deals</a>',
      '<a href="@Context.Links.TodaysDeals.Href">Deals</a>\n        @foreach (var category in Context.Search.Categories)\n        {\n            <a href="@Context.Links.Category(category.Href)">@category.Label</a>\n        }'
    )
    .replace('<a href="@Context.Links.Cart.Href" aria-label="Cart">@Context.Links.Cart.Label</a>', '<a class="sfb-cart-badge" href="@Context.Links.Cart.Href" aria-label="Cart">@Context.Links.Cart.Label <span data-storefront-cart-badge hidden>0</span></a>')
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
    .replace("<h1>", '<h1 class="sfb-product-page">')
    .replace(
      '<ProductDetailShell ProductName="@Context.Product.Name" />',
      '<ProductDetailShell ProductName="@Context.Product.Name" PurchasePanel="@Context.PurchasePanel" PurchaseActions="@Context.PurchaseActions" />'
    );
}

function transformGallery(content) {
  return content.replace('class="starter-gallery-placeholder"', 'class="starter-gallery-placeholder sfb-product-gallery"');
}

function transformPurchasePanel(content) {
  if (content.includes("data-storefront-product-purchase")) {
    return content;
  }

  return content
    .replace(
      '<aside class="starter-purchase-panel">',
      '@using BlazorShop.Storefront.Components.Contracts.Product\n@using BlazorShop.Storefront.Components.Headless.Product\n\n<aside class="starter-purchase-panel sfb-product-purchase"\n       data-storefront-product-purchase\n       data-selection-preview-route="@PurchaseActions.SelectionPreviewRoute"\n       data-product-id="@PurchasePanel.ProductId"\n       data-product-name="@PurchasePanel.ProductName"\n       data-resolved-variant-id="@PurchasePanel.ResolvedVariantId"\n       data-currency-code="@PurchasePanel.CurrencyCode">'
    )
    .replace('<aside class="starter-purchase-panel">', '<aside class="starter-purchase-panel sfb-product-purchase">')
    .replace(
      '<button class="starter-button" type="button" disabled>Add to cart</button>',
      '<label class="sfb-quantity-control">Quantity <input data-storefront-purchase-quantity type="number" min="@PurchasePanel.MinOrderQuantity" max="@PurchasePanel.MaxOrderQuantity" value="@PurchasePanel.MinOrderQuantity" /></label>\n    <button class="starter-button"\n            data-storefront-command="cart.add-line"\n            data-storefront-product-purchase-submit\n            data-feedback-target="[data-storefront-purchase-feedback]"\n            type="button"\n            disabled="@(!PurchasePanel.CanSubmitInitialPurchase)">Add to cart</button>\n    <p data-storefront-purchase-feedback aria-live="polite"></p>'
    )
    .replace(
      'public string ProductName { get; set; } = "Product";',
      'public string ProductName { get; set; } = "Product";\n\n    [Parameter]\n    public ProductPurchasePanelModel PurchasePanel { get; set; } = ProductPurchasePanelModel.Empty;\n\n    [Parameter]\n    public ProductPurchaseActionDescriptor PurchaseActions { get; set; } = ProductPurchaseActionDescriptor.Empty;'
    );
}

function transformFallbackPage(content) {
  return content.replace("<h1>", '<h1 class="sfb-fallback-page">');
}

function transformProductDetailShell(content) {
  if (content.includes("PurchasePanel=\"@PurchasePanel\"")) {
    return content;
  }

  return content
    .replace(
      "<PurchasePanelPlaceholder ProductName=\"@ProductName\" />",
      '<PurchasePanelPlaceholder ProductName="@ProductName" PurchasePanel="@PurchasePanel" PurchaseActions="@PurchaseActions" />'
    )
    .replace(
      'public string ProductName { get; set; } = "Product";',
      'public string ProductName { get; set; } = "Product";\n\n    [Parameter]\n    public BlazorShop.Storefront.Components.Contracts.Product.ProductPurchasePanelModel PurchasePanel { get; set; } = BlazorShop.Storefront.Components.Contracts.Product.ProductPurchasePanelModel.Empty;\n\n    [Parameter]\n    public BlazorShop.Storefront.Components.Headless.Product.ProductPurchaseActionDescriptor PurchaseActions { get; set; } = BlazorShop.Storefront.Components.Headless.Product.ProductPurchaseActionDescriptor.Empty;'
    );
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
