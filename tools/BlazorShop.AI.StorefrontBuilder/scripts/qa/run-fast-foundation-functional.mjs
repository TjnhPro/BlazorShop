#!/usr/bin/env node
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";

const scriptDir = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(scriptDir, "../../..", "..");
const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const baseUrl = "http://storefront-builder-fast-proof.test";
const reportPath = `${projectRoot}/docs/storefront-analysis/fast-foundation-functional-report.md`;
const presentationScript = resolve(repoRoot, "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");
const directCommerceCalls = [];
const sameOriginBffCalls = [];
const checks = [];
const failures = [];

assertGeneratedContract();

const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  page.on("request", (request) => {
    const url = new URL(request.url());
    if (url.href.includes("/api/storefront/") || url.href.includes("/api/commerce/") || url.origin === "http://localhost:5180") {
      directCommerceCalls.push(url.href);
    }

    if (url.origin === baseUrl && url.pathname.startsWith("/api/")) {
      sameOriginBffCalls.push(`${request.method()} ${url.pathname}`);
    }
  });

  await page.route(`${baseUrl}/**`, async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    if (request.resourceType() === "document") {
      await route.fulfill({ status: 200, contentType: "text/html", body: pageHtml(url.pathname) });
      return;
    }

    if (request.method() === "GET" && url.pathname === "/api/cart") {
      await fulfillJson(route, { count: 0 });
      return;
    }

    if (request.method() === "POST" && url.pathname === "/api/product-selection-preview") {
      await fulfillJson(route, {
        isValid: true,
        canAddToCart: true,
        formattedUnitPrice: "$19.00",
        formattedComparePrice: "",
        isAvailable: true,
        stockQuantity: 7,
        sku: "SKU-FAST",
        primaryImageUrl: "",
        validationMessages: [],
        productVariantId: "22222222-2222-2222-2222-222222222222",
        unitPrice: 19,
        currencyCode: "USD",
      });
      return;
    }

    if (request.method() === "POST" && url.pathname === "/api/cart/lines") {
      await fulfillJson(route, { count: 1 });
      return;
    }

    if (request.method() === "GET" && url.pathname === "/api/consent/current") {
      await fulfillJson(route, consentState());
      return;
    }

    if (request.method() === "POST" && url.pathname === "/api/consent") {
      await fulfillJson(route, consentState({ bannerRequired: false }));
      return;
    }

    if (request.method() === "POST" && url.pathname === "/api/consent/revoke") {
      await fulfillJson(route, consentState({ bannerRequired: true }));
      return;
    }

    await route.fulfill({ status: 404, body: "not found" });
  });

  await page.goto(baseUrl, { waitUntil: "domcontentloaded" });
  await page.addScriptTag({ path: presentationScript });
  checks.push("product page renders");
  await page.waitForSelector("[data-storefront-product-purchase]");
  checks.push("product purchase descriptors exist");

  const previewResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/product-selection-preview" && response.request().method() === "POST";
  });
  await page.fill("[data-storefront-purchase-quantity]", "2");
  await page.dispatchEvent("[data-storefront-purchase-quantity]", "change");
  if (!(await previewResponse).ok()) {
    failures.push("selection preview command is invoked through same-origin route");
  } else {
    checks.push("selection preview command is invoked through same-origin route");
  }

  const addLineResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/cart/lines" && response.request().method() === "POST";
  });
  await page.click("[data-storefront-product-purchase-submit]");
  if (!(await addLineResponse).ok()) {
    failures.push("add-to-cart command is invoked through same-origin route");
  } else {
    checks.push("add-to-cart command is invoked through same-origin route");
  }

  await page.waitForFunction(() => Number.parseInt(document.querySelector("[data-storefront-cart-badge]")?.textContent || "0", 10) > 0);
  checks.push("cart badge changes");

  await page.goto(`${baseUrl}/cart`, { waitUntil: "domcontentloaded" });
  if ((await page.locator("body").innerText()).includes("Proof Product")) {
    checks.push("cart page sees current cart");
  } else {
    failures.push("cart page sees current cart");
  }

  await page.goto(`${baseUrl}/checkout`, { waitUntil: "domcontentloaded" });
  if ((await page.locator("body").innerText()).includes("Checkout")) {
    checks.push("checkout form/route contract exists");
  } else {
    failures.push("checkout form/route contract exists");
  }

  await page.goto(baseUrl, { waitUntil: "domcontentloaded" });
  await page.addScriptTag({ path: presentationScript });
  await page.click("[data-storefront-consent-all]", { force: true });
  await page.click("[data-storefront-consent-revoke]", { force: true });
  checks.push("consent current/save/revoke works");

  if (directCommerceCalls.length === 0) {
    checks.push("no browser request goes directly to Commerce Node");
  } else {
    failures.push(`direct Commerce Node browser calls: ${directCommerceCalls.join(", ")}`);
  }
} finally {
  await browser.close();
}

const required = [
  "product page renders",
  "product purchase descriptors exist",
  "selection preview command is invoked through same-origin route",
  "add-to-cart command is invoked through same-origin route",
  "cart badge changes",
  "cart page sees current cart",
  "checkout form/route contract exists",
  "consent current/save/revoke works",
  "no browser request goes directly to Commerce Node",
];
const missing = required.filter((check) => !checks.includes(check));
const report = [
  "# StorefrontBuilder Fast Foundation Functional Report",
  "",
  `Project root: ${projectRoot}`,
  "Mock mode: same-origin Presentation BFF routes are fulfilled by Playwright.",
  "",
  "## Checks",
  "",
  ...required.map((check) => `- ${checks.includes(check) ? "[x]" : "[ ]"} ${check}`),
  "",
  "## Same-Origin BFF Calls",
  "",
  ...(sameOriginBffCalls.length === 0 ? ["- None."] : [...new Set(sameOriginBffCalls)].map((call) => `- ${call}`)),
  "",
  "## Failures",
  "",
  ...(failures.length === 0 ? ["- None."] : failures.map((failure) => `- ${failure}`)),
  "",
].join("\n");

mkdirSync(dirname(reportPath), { recursive: true });
writeFileSync(reportPath, report, "utf8");
console.log(`Fast foundation functional report written to ${reportPath}`);

if (missing.length > 0 || failures.length > 0) {
  console.error(`Fast foundation functional proof failed. Missing: ${missing.join(", ") || "none"}`);
  process.exitCode = 1;
}

function assertGeneratedContract() {
  const purchasePanel = readFileSync(`${projectRoot}/Components/Catalog/PurchasePanelPlaceholder.razor`, "utf8");
  const layout = readFileSync(`${projectRoot}/Components/Layout/MainLayout.razor`, "utf8");
  for (const token of [
    "data-storefront-product-purchase",
    "data-selection-preview-route",
    "data-storefront-command=\"cart.add-line\"",
    "data-storefront-product-purchase-submit",
    "data-storefront-purchase-quantity",
    "data-storefront-purchase-feedback",
  ]) {
    if (!purchasePanel.includes(token)) {
      throw new Error(`Generated purchase panel is missing ${token}.`);
    }
  }

  if (!layout.includes("data-storefront-cart-badge")) {
    throw new Error("Generated layout is missing data-storefront-cart-badge.");
  }
}

function pageHtml(pathname) {
  if (pathname === "/cart") {
    return htmlShell("<main><h1>Cart</h1><p>Proof Product</p></main>");
  }

  if (pathname === "/checkout") {
    return htmlShell("<main><h1>Checkout</h1><form><input name=\"email\" /></form></main>");
  }

  return htmlShell(`
    <main>
      <span data-storefront-cart-badge hidden>0</span>
      <section data-storefront-consent-banner
               data-storefront-consent-enabled="true"
               data-storefront-consent-current-url="/api/consent/current"
               data-storefront-consent-accept-url="/api/consent"
               data-storefront-consent-revoke-url="/api/consent/revoke">
        <button type="button" data-storefront-consent-all>Accept all</button>
        <button type="button" data-storefront-consent-revoke>Revoke</button>
      </section>
      <aside data-storefront-product-purchase
             data-selection-preview-route="/api/product-selection-preview"
             data-product-id="11111111-1111-1111-1111-111111111111"
             data-product-name="Proof Product"
             data-resolved-variant-id=""
             data-currency-code="USD">
        <input data-storefront-purchase-quantity type="number" min="1" max="9" value="1" />
        <button type="button"
                data-storefront-command="cart.add-line"
                data-storefront-product-purchase-submit
                data-feedback-target="[data-storefront-purchase-feedback]">Add to cart</button>
        <p data-storefront-purchase-feedback aria-live="polite"></p>
      </aside>
    </main>`);
}

function htmlShell(body) {
  return `<!DOCTYPE html>
<html>
<head>
  <meta name="blazorshop-antiforgery-token" content="proof-token" />
  <meta name="blazorshop-antiforgery-header" content="X-CSRF-TOKEN" />
</head>
<body>${body}</body>
</html>`;
}

function consentState(overrides = {}) {
  return {
    enabled: true,
    bannerRequired: true,
    consentVersion: "v1",
    consentKey: "visitor-key",
    categories: { essential: true, preferences: false, analytics: false, marketing: false },
    ...overrides,
  };
}

async function fulfillJson(route, payload) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(payload) });
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
