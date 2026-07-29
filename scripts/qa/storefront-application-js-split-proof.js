const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const repoRoot = path.resolve(__dirname, "../..");
const presentationScript = path.join(repoRoot, "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");
const visualScript = path.join(repoRoot, "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

const baseUrl = "http://storefront-js-proof.test";
const requests = [];

function consentState(overrides = {}) {
  return {
    enabled: true,
    bannerRequired: true,
    consentVersion: "v1",
    consentKey: "visitor-key",
    categories: {
      essential: true,
      preferences: false,
      analytics: false,
      marketing: false,
    },
    ...overrides,
  };
}

function pageHtml() {
  return `<!DOCTYPE html>
<html>
<head>
  <meta name="blazorshop-antiforgery-token" content="proof-token" />
  <meta name="blazorshop-antiforgery-header" content="X-CSRF-TOKEN" />
</head>
<body>
  <span data-storefront-cart-badge hidden>0</span>
  <button data-storefront-consent-manage type="button">Manage cookies</button>
  <section data-storefront-consent-banner
           data-storefront-consent-enabled="true"
           data-storefront-consent-current-url="/api/consent/current"
           data-storefront-consent-accept-url="/api/consent"
           data-storefront-consent-revoke-url="/api/consent/revoke"
           data-storefront-consent-current-method="GET"
           data-storefront-consent-accept-method="POST"
           data-storefront-consent-revoke-method="POST"
           data-storefront-consent-changed-event="storefront:consent:changed"
           data-storefront-consent-manage-event="storefront:consent:manage-requested"
           class="starter-consent-banner hidden">
    <input type="checkbox" data-storefront-consent-preferences />
    <input type="checkbox" data-storefront-consent-analytics />
    <input type="checkbox" data-storefront-consent-marketing />
    <button type="button" data-storefront-consent-essential>Essential only</button>
    <button type="button" data-storefront-consent-selected>Save selected</button>
    <button type="button" data-storefront-consent-all>Accept all</button>
    <button type="button" data-storefront-consent-revoke>Revoke</button>
  </section>
  <main>
    <span data-storefront-selection-price></span>
    <span data-storefront-selection-compare></span>
    <span data-storefront-selection-stock></span>
    <span data-storefront-selection-sku></span>
    <span data-storefront-selection-gtin></span>
    <div id="purchase"
         data-storefront-product-purchase
         data-selection-preview-route="/api/product-selection-preview"
         data-product-id="11111111-1111-1111-1111-111111111111"
         data-product-name="Proof Product"
         data-currency-code="USD">
      <input type="radio"
             name="Color"
             value="Black"
             checked
             data-storefront-purchase-attribute
             data-storefront-purchase-attribute-name="Color" />
      <input type="number" value="2" data-storefront-purchase-quantity />
      <button type="button"
              data-storefront-command="cart.add-line"
              data-storefront-product-purchase-submit
              data-default-label="Add to Cart"
              data-success-label="Added"
              data-feedback-target="#product-cart-feedback">
        Add to Cart
      </button>
      <button type="button" data-storefront-product-purchase-submit data-proof-missing-command>Missing command</button>
      <button type="button" data-storefront-command="cart.remove-line" data-storefront-product-purchase-submit data-proof-wrong-command>Wrong command</button>
      <p id="product-cart-feedback" data-storefront-selection-message></p>
    </div>
  </main>
  <div data-storefront-toast-region></div>
  <template data-storefront-toast-template>
    <div data-storefront-toast>
      <span data-storefront-toast-accent></span>
      <strong data-storefront-toast-heading></strong>
      <span data-storefront-toast-message></span>
      <button type="button" data-storefront-toast-close>Close</button>
    </div>
  </template>
  <script>
    window.__storefrontProofEvents = [];
    ["storefront:cart:changed", "storefront:cart:error", "storefront:consent:changed", "storefront:consent:manage-requested", "storefront:product-selection:changed", "storefront:product-selection:error", "storefront:product-purchase:selection-changed", "storefront:product-purchase:add-line-succeeded", "storefront:product-purchase:add-line-failed", "blazorshop:cart-changed"]
      .forEach((name) => document.addEventListener(name, (event) => window.__storefrontProofEvents.push({ name, detail: event.detail })));
  </script>
</body>
</html>`;
}

async function parseRequest(route) {
  const request = route.request();
  const url = new URL(request.url());
  const bodyText = request.postData() || "";
  const body = bodyText ? JSON.parse(bodyText) : null;
  const entry = {
    method: request.method(),
    path: url.pathname,
    csrf: request.headers()["x-csrf-token"] || "",
    body,
  };
  requests.push(entry);
  return entry;
}

async function fulfillJson(route, payload) {
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(payload),
  });
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  try {
    await page.route(`${baseUrl}/**`, async (route) => {
      const url = new URL(route.request().url());
      if (route.request().resourceType() === "document") {
        await route.fulfill({ status: 200, contentType: "text/html", body: pageHtml() });
        return;
      }

      const entry = await parseRequest(route);
      if (entry.method === "GET" && entry.path === "/api/cart") {
        await fulfillJson(route, { count: 0 });
        return;
      }

      if (entry.method === "GET" && entry.path === "/api/consent/current") {
        await fulfillJson(route, consentState());
        return;
      }

      if (entry.method === "POST" && entry.path === "/api/consent") {
        if (entry.csrf !== "proof-token") {
          throw new Error("Consent POST did not include antiforgery header.");
        }

        await fulfillJson(route, consentState({ bannerRequired: false, categories: { essential: true, ...entry.body } }));
        return;
      }

      if (entry.method === "POST" && entry.path === "/api/consent/revoke") {
        if (entry.csrf !== "proof-token") {
          throw new Error("Consent revoke did not include antiforgery header.");
        }

        await fulfillJson(route, consentState({ bannerRequired: true, revokedAtUtc: new Date().toISOString() }));
        return;
      }

      if (entry.method === "POST" && entry.path === "/api/product-selection-preview") {
        if (entry.csrf !== "proof-token") {
          throw new Error("Product selection preview did not include antiforgery header.");
        }

        await fulfillJson(route, {
          isValid: true,
          canAddToCart: true,
          formattedUnitPrice: "$19.00",
          formattedComparePrice: "",
          isAvailable: true,
          stockQuantity: 7,
          sku: "SKU-PROOF",
          gtin: "GTIN-PROOF",
          primaryImageUrl: "",
          validationMessages: [],
          productVariantId: "22222222-2222-2222-2222-222222222222",
          unitPrice: 19,
          currencyCode: "USD",
        });
        return;
      }

      if (entry.method === "POST" && entry.path === "/api/cart/lines") {
        if (entry.csrf !== "proof-token") {
          throw new Error("Cart add-line did not include antiforgery header.");
        }

        await fulfillJson(route, { count: 1 });
        return;
      }

      await route.fulfill({ status: 404, body: "not found" });
    });

    await page.goto(baseUrl, { waitUntil: "domcontentloaded" });
    await page.addScriptTag({ path: presentationScript });
    await page.addScriptTag({ path: visualScript });
    await page.evaluate(() => {
      if (window.blazorShopStorefront.application || window.blazorShopStorefront.bindings) {
        throw new Error("Presentation script exposed command-capable public internals.");
      }

      window.blazorShopStorefront.initialize();
      window.blazorShopStorefront.initialize();
    });

    await page.waitForFunction(() => window.__storefrontProofEvents.some((event) => event.name === "storefront:consent:changed"));
    await page.click("[data-storefront-consent-manage]");
    await page.waitForFunction(() => window.__storefrontProofEvents.some((event) => event.name === "storefront:consent:manage-requested"));
    await page.click("[data-storefront-consent-selected]");
    await page.waitForFunction(() => window.__storefrontProofEvents.filter((event) => event.name === "storefront:consent:changed").length >= 2);
    await page.click("[data-storefront-consent-revoke]");
    await page.waitForFunction(() => window.__storefrontProofEvents.filter((event) => event.name === "storefront:consent:changed").length >= 3);

    const previewAfterQuantityChange = page.waitForRequest((request) => {
      const url = new URL(request.url());
      if (request.method() !== "POST" || url.pathname !== "/api/product-selection-preview") {
        return false;
      }

      const bodyText = request.postData() || "";
      return bodyText ? JSON.parse(bodyText).Quantity === 3 : false;
    });
    await page.fill("[data-storefront-purchase-quantity]", "3");
    await previewAfterQuantityChange;
    await page.waitForFunction(() => document.querySelector("[data-storefront-selection-price]")?.textContent === "$19.00");
    await page.evaluate(() => {
      const selectionEvent = window.__storefrontProofEvents.find((event) => event.name === "storefront:product-purchase:selection-changed");
      if (!selectionEvent) {
        throw new Error("Missing product purchase selection event.");
      }

      assertPublicSelectionEvent(selectionEvent.detail);
      const genericSelectionEvent = window.__storefrontProofEvents.find((event) => event.name === "storefront:product-selection:changed");
      if (!genericSelectionEvent) {
        throw new Error("Missing generic product selection event.");
      }

      assertPublicSelectionEvent(genericSelectionEvent.detail);

      function assertPublicSelectionEvent(detail) {
        if (!detail || typeof detail !== "object") {
          throw new Error("Selection event detail missing.");
        }

        if ("preview" in detail) {
          throw new Error("Selection event leaked raw preview.");
        }

        const selection = detail.selection;
        if (!selection || typeof selection !== "object") {
          throw new Error("Selection event missing public projection.");
        }

        for (const forbidden of ["productId", "productVariantId", "selectedAttributes", "quantity", "currencyCode", "unitPrice", "available"]) {
          if (forbidden in selection) {
            throw new Error(`Selection projection leaked ${forbidden}.`);
          }
        }

        for (const required of ["ready", "valid", "priceText", "comparePriceText", "stockText", "skuText", "gtinText", "mainImageUrl", "message"]) {
          if (!(required in selection)) {
            throw new Error(`Selection projection missing ${required}.`);
          }
        }
      }
    });

    await page.click("[data-proof-missing-command]");
    await page.waitForFunction(() => document.querySelector("#product-cart-feedback")?.textContent?.includes("Missing storefront command descriptor."));
    await page.click("[data-proof-wrong-command]");
    await page.waitForFunction(() => document.querySelector("#product-cart-feedback")?.textContent?.includes("Unsupported storefront command"));
    if (requests.some((request) => request.path === "/api/cart/lines")) {
      throw new Error("Malformed command descriptors must not execute add-to-cart.");
    }

    await page.click('[data-storefront-command="cart.add-line"][data-storefront-product-purchase-submit]');
    await page.waitForFunction(() => document.querySelector("[data-storefront-cart-badge]")?.textContent === "1");
    await page.evaluate(() => {
      const cartChanged = window.__storefrontProofEvents.find((event) => event.name === "storefront:cart:changed" && event.detail?.count === 1);
      if (!cartChanged) {
        throw new Error("Cart changed event did not publish canonical count.");
      }

      if ("summary" in cartChanged.detail) {
        throw new Error("Cart changed event leaked raw cart summary.");
      }

      const addSucceeded = window.__storefrontProofEvents.find((event) => event.name === "storefront:product-purchase:add-line-succeeded");
      if (!addSucceeded || addSucceeded.detail?.count !== 1) {
        throw new Error("Add-line success event did not publish count.");
      }

      if ("summary" in addSucceeded.detail || "selection" in addSucceeded.detail) {
        throw new Error("Add-line success event leaked raw summary or selection state.");
      }

      if (window.__storefrontProofEvents.some((event) => event.name === "blazorshop:cart-changed")) {
        throw new Error("Legacy cart changed event should not be published.");
      }
    });

    const previewRequest = requests.filter((request) => request.path === "/api/product-selection-preview").at(-1);
    if (!previewRequest || previewRequest.body?.Quantity !== 3) {
      throw new Error(`Product selection preview request did not carry the visual selection payload: ${JSON.stringify(previewRequest?.body)}`);
    }

    const addLineRequests = requests.filter((request) => request.path === "/api/cart/lines");
    if (addLineRequests.length !== 1) {
      throw new Error(`Expected exactly one add-to-cart request after repeated initialize calls, got ${addLineRequests.length}.`);
    }

    const addLineRequest = addLineRequests[0];
    if (!addLineRequest || addLineRequest.body?.Quantity !== 3 || addLineRequest.body?.CurrencyCode !== "USD") {
      throw new Error("Add-to-cart request did not carry the expected command payload.");
    }

    if ("UnitPrice" in addLineRequest.body || "unitPrice" in addLineRequest.body) {
      throw new Error("Add-to-cart command leaked client-supplied unit price.");
    }

    const addLineCountBeforeEnhancedNavigation = requests.filter((request) => request.path === "/api/cart/lines").length;
    const enhancedPreviewRequest = page.waitForRequest((request) => {
      const url = new URL(request.url());
      if (request.method() !== "POST" || url.pathname !== "/api/product-selection-preview") {
        return false;
      }

      const bodyText = request.postData() || "";
      return bodyText ? JSON.parse(bodyText).Quantity === 4 : false;
    });
    await page.evaluate(() => {
      document.querySelector("main").innerHTML = `
        <span data-storefront-cart-badge hidden>0</span>
        <span data-storefront-selection-price></span>
        <span data-storefront-selection-compare></span>
        <span data-storefront-selection-stock></span>
        <span data-storefront-selection-sku></span>
        <span data-storefront-selection-gtin></span>
        <div id="enhanced-purchase"
             data-storefront-product-purchase
             data-selection-preview-route="/api/product-selection-preview"
             data-product-id="33333333-3333-3333-3333-333333333333"
             data-product-name="Enhanced Product"
             data-currency-code="USD">
          <input type="number" value="4" data-storefront-purchase-quantity />
          <button type="button"
                  data-storefront-command="cart.add-line"
                  data-storefront-product-purchase-submit
                  data-feedback-target="#enhanced-cart-feedback">
            Add to Cart
          </button>
          <p id="enhanced-cart-feedback" data-storefront-selection-message></p>
        </div>`;
      document.dispatchEvent(new Event("enhancedload"));
      document.dispatchEvent(new Event("enhancedload"));
    });
    await enhancedPreviewRequest;
    await page.waitForFunction(() => document.querySelector("[data-storefront-selection-price]")?.textContent === "$19.00");
    await page.click("#enhanced-purchase [data-storefront-product-purchase-submit]");
    await page.waitForFunction(() => document.querySelector("main [data-storefront-cart-badge]")?.textContent === "1");
    const enhancedAddLineCount = requests.filter((request) => request.path === "/api/cart/lines").length - addLineCountBeforeEnhancedNavigation;
    if (enhancedAddLineCount !== 1) {
      throw new Error(`Enhanced navigation product submitted ${enhancedAddLineCount} add-to-cart requests.`);
    }

    const eventNames = await page.evaluate(() => window.__storefrontProofEvents.map((event) => event.name));
    for (const expected of ["storefront:cart:changed", "storefront:consent:changed", "storefront:consent:manage-requested", "storefront:product-selection:changed"]) {
      if (!eventNames.includes(expected)) {
        throw new Error(`Missing storefront application event ${expected}.`);
      }
    }
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
