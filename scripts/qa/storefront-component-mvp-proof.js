const fs = require("fs");
const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const baseUrl = trimEnd(process.env.STOREFRONT_BASE_URL || "http://127.0.0.1:18640", "/");
const phase = (process.env.STOREFRONT_COMPONENT_MVP_PHASE || "raw-html").toLowerCase();
const artifactRoot = path.resolve(__dirname, "../../output/playwright/storefront-component-mvp");
const steps = [];
const directCommerceCalls = [];
const sameOriginCalls = [];
const consoleErrors = [];
const pageErrors = [];

async function main() {
  fs.mkdirSync(artifactRoot, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });

  try {
    if (!["raw-html", "hybrid"].includes(phase)) {
      throw new Error(`Unsupported Component MVP proof phase '${phase}'.`);
    }

    if (phase === "raw-html") {
      await assertRawHtml(context);
    } else if (phase === "hybrid") {
      await assertHydratedHybrid(context);
    }

    const evidence = {
      ok: true,
      phase,
      baseUrl,
      generatedAtUtc: new Date().toISOString(),
      steps,
      sameOriginCalls: [...new Set(sameOriginCalls)],
      directCommerceCalls,
      consoleErrors,
      pageErrors,
    };

    fs.writeFileSync(path.join(artifactRoot, `${phase}.evidence.json`), JSON.stringify(evidence, null, 2));
    console.log(JSON.stringify(evidence, null, 2));
  } finally {
    await context.close();
    await browser.close();
  }
}

async function assertHydratedHybrid(context) {
  const page = await context.newPage();
  attachRuntimeGuards(page);
  await mockRailSuccess(page);

  const response = await page.goto(`${baseUrl}/__qa/component-mvp`, {
    waitUntil: "domcontentloaded",
    timeout: 60000,
  });
  assert(response && response.status() === 200, `/__qa/component-mvp returned HTTP ${response ? response.status() : "no response"}`);

  const root = page.locator('[data-storefront-component="hybrid-runtime-probe"]').first();
  await root.waitFor({ state: "attached", timeout: 30000 });
  await page.waitForFunction(() => {
    const element = document.querySelector('[data-storefront-component="hybrid-runtime-probe"]');
    return element?.getAttribute("data-storefront-runtime-state") === "interactive";
  }, null, { timeout: 60000 });

  const value = page.locator("[data-storefront-hybrid-value]").first();
  await expectText(page, value, "0", "initial hybrid counter");
  await page.locator("[data-storefront-hybrid-action]").first().click();
  await expectText(page, value, "1", "hybrid counter after first click");
  await page.locator("[data-storefront-hybrid-action]").first().click();
  await expectText(page, value, "2", "hybrid counter after second click");

  assertNoRuntimeFailures();
  assert(
    !directCommerceCalls.some((call) => call.includes("hybrid-runtime-probe")),
    `direct Commerce request during hybrid proof: ${directCommerceCalls.join(", ")}`,
  );
  steps.push({ step: "component-mvp.hybrid-interactive-click", ok: true });
}

async function assertRawHtml(context) {
  const response = await context.request.get(`${baseUrl}/__qa/component-mvp`, { timeout: 30000 });
  assert(response.status() === 200, `/__qa/component-mvp returned HTTP ${response.status()}`);
  const html = await response.text();

  assert(html.includes("data-storefront-component-mvp"), "component MVP root marker missing from raw HTML");
  assert(html.includes('data-storefront-component="brand-logo"'), "SSR brand logo marker missing from raw HTML");
  assert(html.includes("data-storefront-brand"), "SSR brand metadata marker missing from raw HTML");
  assert(html.includes('data-storefront-component="hybrid-runtime-probe"'), "Hybrid probe marker missing from raw HTML");
  assert(html.includes('data-storefront-runtime-state="prerender"'), "Hybrid prerender marker missing from raw HTML");
  assert(html.includes("data-storefront-hybrid-value"), "Hybrid value marker missing from raw HTML");
  assert(html.includes("Prerendered before WebAssembly"), "Hybrid useful prerender copy missing from raw HTML");
  assert(html.includes("name=\"robots\""), "robots meta tag missing from raw HTML");
  assert(html.includes("noindex"), "noindex metadata missing from raw HTML");

  steps.push({
    step: "component-mvp.raw-html",
    ok: true,
    status: response.status(),
  });
}

function trimEnd(value, suffix) {
  return value.endsWith(suffix) ? value.slice(0, -suffix.length) : value;
}

async function mockRailSuccess(page) {
  await page.route("**/api/catalog/discounted-products?**", async (route) => {
    sameOriginCalls.push(`GET ${new URL(route.request().url()).pathname}`);
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        products: [{
          id: "11111111-1111-4111-8111-111111111111",
          name: "Component MVP Product",
          productUrl: "/product/component-mvp-product",
          categoryName: "QA",
          categoryUrl: "/category/qa",
          imageUrl: "",
          description: "Deterministic Component MVP product.",
          priceDisplay: "USD 10.00",
          comparePriceDisplay: "USD 20.00",
          hasVariants: false,
          inStock: true,
          isNewArrival: false,
          purchasable: true,
          purchaseUrl: "/product/component-mvp-product#purchase",
          canAddDirectly: false,
          unitPriceValue: "10.00",
          currencyCode: "USD",
          directAddStockValue: 1,
          purchaseBlockMessage: null,
          purchasePaused: false,
        }],
        success: true,
        retryable: false,
      }),
    });
  });
}

function attachRuntimeGuards(page) {
  page.on("request", (request) => {
    const url = new URL(request.url());
    if (url.origin === baseUrl && url.pathname.startsWith("/api/")) {
      sameOriginCalls.push(`${request.method()} ${url.pathname}`);
    }

    if (url.href.includes("/api/storefront/") || url.href.includes("/api/commerce/") || url.origin === "http://localhost:5180") {
      directCommerceCalls.push(`${request.method()} ${url.href}`);
    }
  });

  page.on("console", (message) => {
    if (message.type() === "error") {
      consoleErrors.push(message.text());
    }
  });

  page.on("pageerror", (error) => {
    pageErrors.push(error.message);
  });
}

async function expectText(page, locator, expected, label) {
  await locator.waitFor({ state: "attached", timeout: 15000 });
  await page.waitForFunction(
    ({ selector, value }) => document.querySelector(selector)?.textContent?.trim() === value,
    { selector: "[data-storefront-hybrid-value]", value: expected },
    { timeout: 15000 },
  );
  const actual = (await locator.textContent())?.trim();
  assert(actual === expected, `${label} expected '${expected}' but got '${actual}'`);
}

function assertNoRuntimeFailures() {
  assert(pageErrors.length === 0, `page errors detected: ${pageErrors.join(" | ")}`);
  assert(consoleErrors.length === 0, `console errors detected: ${consoleErrors.join(" | ")}`);
  assert(directCommerceCalls.length === 0, `direct Commerce browser calls detected: ${directCommerceCalls.join(", ")}`);
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

main().catch((error) => {
  const failure = {
    ok: false,
    phase,
    baseUrl,
    error: error.stack || String(error),
    steps,
    sameOriginCalls: [...new Set(sameOriginCalls)],
    directCommerceCalls,
    consoleErrors,
    pageErrors,
  };
  fs.mkdirSync(artifactRoot, { recursive: true });
  fs.writeFileSync(path.join(artifactRoot, `${phase}.evidence.failed.json`), JSON.stringify(failure, null, 2));
  console.error(error);
  process.exit(1);
});
