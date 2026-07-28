#!/usr/bin/env node
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
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

writeFunctionalBrowserBridge(projectRoot);

console.log("StorefrontBuilder composition applied shell, home, catalog, product, and fallback page files from generation-plan.yaml.");
console.log("Commerce commands remain bound through Starter slot/action contracts such as cart.add-line.");

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
    .replace('<a href="@Context.Links.Cart.Href" aria-label="Cart">@Context.Links.Cart.Label</a>', '<a class="sfb-cart-badge" href="@Context.Links.Cart.Href" aria-label="Cart">@Context.Links.Cart.Label <span>0</span></a>')
    .replace(
      "</header>",
      '<nav class="sfb-mobile-nav" aria-label="Mobile navigation"><a href="@Context.Links.Home.Href">@Context.Links.Home.Label</a><a href="@Context.Links.Cart.Href">@Context.Links.Cart.Label</a><a href="@Context.Links.AccountRoot.Href">@Context.Links.AccountRoot.Label</a></nav>\n</header>'
    );
}

function transformApplicationHead(content) {
  if (content.includes("css/storefront-builder.generated.css") && content.includes("js/storefront-builder.functional.js")) {
    return content;
  }

  let updated = content;
  if (!updated.includes("css/storefront-builder.generated.css")) {
    updated = updated.replace(
      '<link rel="stylesheet" href="css/starter.css" />',
      '<link rel="stylesheet" href="css/starter.css" />\n    <link rel="stylesheet" href="css/storefront-builder.generated.css" />'
    );
  }

  if (!updated.includes("js/storefront-builder.functional.js")) {
    if (updated.includes("</head>")) {
      updated = updated.replace(
        "</head>",
        '    <script src="js/storefront-builder.functional.js" defer></script>\n</head>'
      );
    } else {
      updated = updated.replace(
        '<link rel="stylesheet" href="css/storefront-builder.generated.css" />',
        '<link rel="stylesheet" href="css/storefront-builder.generated.css" />\n<script src="js/storefront-builder.functional.js" defer></script>'
      );
    }
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
  if (content.includes('data-action="cart.add-line"')) {
    return content;
  }

  return content
    .replace(
      '<aside class="starter-purchase-panel">',
      '@using BlazorShop.Storefront.Components.Contracts.Product\n@using BlazorShop.Storefront.Components.Headless.Product\n\n<aside class="starter-purchase-panel sfb-product-purchase"\n       data-storefront-selection-preview\n       data-preview-route="@PurchaseActions.SelectionPreviewRoute"\n       data-product-id="@PurchasePanel.ProductId"\n       data-currency-code="@PurchasePanel.CurrencyCode">'
    )
    .replace('<aside class="starter-purchase-panel">', '<aside class="starter-purchase-panel sfb-product-purchase">')
    .replace(
      '<button class="starter-button" type="button" disabled>Add to cart</button>',
      '<label class="sfb-quantity-control">Quantity <input data-storefront-generated-quantity type="number" min="@PurchasePanel.MinOrderQuantity" max="@PurchasePanel.MaxOrderQuantity" value="@PurchasePanel.MinOrderQuantity" /></label>\n    <button class="starter-button"\n            data-action="cart.add-line"\n            data-storefront-generated-add-to-cart\n            data-product-id="@PurchasePanel.ProductId"\n            data-product-name="@PurchasePanel.ProductName"\n            data-resolved-variant-id="@PurchasePanel.ResolvedVariantId"\n            data-currency-code="@PurchasePanel.CurrencyCode"\n            data-quantity-selector="[data-storefront-generated-quantity]"\n            data-feedback-target="[data-storefront-selection-message]"\n            type="button"\n            disabled="@(!PurchasePanel.CanSubmitInitialPurchase)">Add to cart</button>\n    <p data-storefront-selection-message aria-live="polite"></p>'
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

function writeFunctionalBrowserBridge(rootPath) {
  const output = join(rootPath, "wwwroot", "js", "storefront-builder.functional.js");
  mkdirSync(join(rootPath, "wwwroot", "js"), { recursive: true });
  writeFileSync(output, `(() => {
  const root = window.blazorShopStorefront = window.blazorShopStorefront || {};

  function readQuantity(button) {
    const selector = button.dataset.quantitySelector;
    const input = selector ? document.querySelector(selector) : null;
    const value = Number.parseInt(input?.value || "1", 10);
    return Number.isFinite(value) && value > 0 ? value : 1;
  }

  function optionalGuid(value) {
    return value && value !== "00000000-0000-0000-0000-000000000000" ? value : null;
  }

  function writeFeedback(button, text) {
    const target = button.dataset.feedbackTarget ? document.querySelector(button.dataset.feedbackTarget) : null;
    if (target) {
      target.textContent = text;
    }
  }

  function updateCartBadge(summary) {
    const count = Number.parseInt(summary?.count ?? summary?.Count ?? "0", 10);
    if (!Number.isFinite(count)) {
      return;
    }

    document.querySelectorAll(".sfb-cart-badge span, [data-storefront-cart-badge]").forEach((badge) => {
      badge.textContent = count > 99 ? "99+" : String(count);
      badge.hidden = count <= 0;
      badge.classList.toggle("hidden", count <= 0);
    });
  }

  async function addLine(button) {
    const app = root.application;
    if (!app?.cart?.addLine) {
      throw new Error("Storefront application cart bridge is not available.");
    }

    const payload = {
      productId: button.dataset.productId,
      productVariantId: optionalGuid(button.dataset.resolvedVariantId),
      currencyCode: button.dataset.currencyCode || null,
      selectedAttributes: [],
      quantity: readQuantity(button)
    };

    button.disabled = true;
    try {
      const summary = await app.cart.addLine(payload);
      updateCartBadge(summary);
      writeFeedback(button, "Added to cart.");
    } finally {
      button.disabled = false;
    }
  }

  document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-storefront-generated-add-to-cart]");
    if (!button) {
      return;
    }

    event.preventDefault();
    void addLine(button).catch((error) => writeFeedback(button, error instanceof Error ? error.message : "Cart could not be updated."));
  });
})();\n`, "utf8");
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
