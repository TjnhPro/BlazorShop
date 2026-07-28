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
  <section data-storefront-consent-banner class="hidden">
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
    <div id="purchase"
         data-storefront-selection-preview
         data-preview-route="/api/product-selection-preview"
         data-product-id="11111111-1111-1111-1111-111111111111"
         data-currency-code="USD">
      <input type="radio"
             name="Color"
             value="Black"
             checked
             data-storefront-attribute-control
             data-attribute-name="Color" />
      <input type="number" value="2" data-storefront-selection-quantity />
      <button type="button"
              data-storefront-add-to-cart
              data-default-label="Add to Cart"
              data-success-label="Added"
              data-product-id="11111111-1111-1111-1111-111111111111"
              data-product-name="Proof Product"
              data-currency-code="USD"
              data-preview-container="#purchase"
              data-stock="10"
              data-can-add-to-cart="true"
              data-feedback-target="#product-cart-feedback">
        Add to Cart
      </button>
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
    ["storefront:cart:changed", "storefront:cart:error", "storefront:consent:changed", "storefront:product-selection:changed", "storefront:product-selection:error"]
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

    await page.waitForFunction(() => window.__storefrontProofEvents.some((event) => event.name === "storefront:consent:changed"));
    await page.click("[data-storefront-consent-selected]");
    await page.waitForFunction(() => window.__storefrontProofEvents.filter((event) => event.name === "storefront:consent:changed").length >= 2);
    await page.click("[data-storefront-consent-revoke]");
    await page.waitForFunction(() => window.__storefrontProofEvents.filter((event) => event.name === "storefront:consent:changed").length >= 3);

    await page.fill("[data-storefront-selection-quantity]", "3");
    await page.waitForFunction(() => document.querySelector("[data-storefront-selection-price]")?.textContent === "$19.00");
    await page.click("[data-storefront-add-to-cart]");
    await page.waitForFunction(() => document.querySelector("[data-storefront-cart-badge]")?.textContent === "1");

    const previewRequest = requests.find((request) => request.path === "/api/product-selection-preview");
    if (!previewRequest || previewRequest.body?.Quantity !== 3) {
      throw new Error("Product selection preview request did not carry the visual selection payload.");
    }

    const addLineRequest = requests.find((request) => request.path === "/api/cart/lines");
    if (!addLineRequest || addLineRequest.body?.Quantity !== 3 || addLineRequest.body?.CurrencyCode !== "USD") {
      throw new Error("Add-to-cart request did not carry the expected command payload.");
    }

    if ("UnitPrice" in addLineRequest.body || "unitPrice" in addLineRequest.body) {
      throw new Error("Add-to-cart command leaked client-supplied unit price.");
    }

    const eventNames = await page.evaluate(() => window.__storefrontProofEvents.map((event) => event.name));
    for (const expected of ["storefront:cart:changed", "storefront:consent:changed", "storefront:product-selection:changed"]) {
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
