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
const credentialLeaks = [];

async function main() {
  fs.mkdirSync(artifactRoot, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });

  try {
    if (!["raw-html", "hybrid", "rail"].includes(phase)) {
      throw new Error(`Unsupported Component MVP proof phase '${phase}'.`);
    }

    if (phase === "raw-html") {
      await assertRawHtml(context);
    } else if (phase === "hybrid") {
      await assertHydratedHybrid(context);
    } else if (phase === "rail") {
      await assertWasmHostRail(context);
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
      credentialLeaks,
    };

    fs.writeFileSync(path.join(artifactRoot, `${phase}.evidence.json`), JSON.stringify(evidence, null, 2));
    console.log(JSON.stringify(evidence, null, 2));
  } finally {
    await context.close();
    await browser.close();
  }
}

async function assertWasmHostRail(context) {
  await assertRailLoadingAndSuccess(context);
  await assertRailSuccess(context);
  await assertRailEmpty(context);
  await assertRailErrorAndRetry(context);
  assertNoRuntimeFailures();
}

async function assertRailLoadingAndSuccess(context) {
  const page = await context.newPage();
  attachRuntimeGuards(page);

  let releaseResponse;
  const release = new Promise((resolve) => { releaseResponse = resolve; });
  await page.route("**/api/catalog/discounted-products?**", async (route) => {
    sameOriginCalls.push(`GET ${new URL(route.request().url()).pathname}`);
    await release;
    await fulfillRailSuccess(route, [createProduct("loading-success-product", "Loading Success Product")]);
  });

  await gotoComponentMvp(page);
  await page.waitForSelector("[data-storefront-product-rail-loading]", { timeout: 60000 });
  releaseResponse();
  await page.waitForSelector("[data-storefront-product-rail-list]", { timeout: 30000 });
  const count = await page.locator("[data-storefront-product-rail-item]").count();
  assert(count === 1, `loading->success rail expected 1 item, got ${count}`);
  steps.push({ step: "component-mvp.rail-loading-state", ok: true });
  await recordStorageCredentialLeaks(page);
  await page.close();
}

async function assertRailSuccess(context) {
  const page = await context.newPage();
  attachRuntimeGuards(page);
  await page.route("**/api/catalog/discounted-products?**", async (route) => {
    sameOriginCalls.push(`GET ${new URL(route.request().url()).pathname}`);
    await fulfillRailSuccess(route, [
      createProduct("success-product-a", "Success Product A"),
      createProduct("success-product-b", "Success Product B"),
    ]);
  });

  await gotoComponentMvp(page);
  await page.waitForSelector("[data-storefront-product-rail-list]", { timeout: 60000 });
  const count = await page.locator("[data-storefront-product-rail-item]").count();
  assert(count === 2, `success rail expected 2 items, got ${count}`);
  steps.push({ step: "component-mvp.rail-success-state", ok: true, itemCount: count });
  await recordStorageCredentialLeaks(page);
  await page.close();
}

async function assertRailEmpty(context) {
  const page = await context.newPage();
  attachRuntimeGuards(page);
  await page.route("**/api/catalog/discounted-products?**", async (route) => {
    sameOriginCalls.push(`GET ${new URL(route.request().url()).pathname}`);
    await fulfillJson(route, { products: [], success: true, retryable: false });
  });

  await gotoComponentMvp(page);
  await page.waitForSelector("[data-storefront-product-rail-empty]", { timeout: 60000 });
  const count = await page.locator("[data-storefront-product-rail-item]").count();
  assert(count === 0, `empty rail should not show stale products, got ${count}`);
  steps.push({ step: "component-mvp.rail-empty-state", ok: true });
  await recordStorageCredentialLeaks(page);
  await page.close();
}

async function assertRailErrorAndRetry(context) {
  const page = await context.newPage();
  attachRuntimeGuards(page);
  let attempts = 0;
  await page.route("**/api/catalog/discounted-products?**", async (route) => {
    sameOriginCalls.push(`GET ${new URL(route.request().url()).pathname}`);
    attempts += 1;
    if (attempts === 1) {
      await fulfillJson(route, {
        products: [],
        success: false,
        code: "component_mvp_rail_outage",
        defaultMessage: "Component MVP simulated rail outage.",
        retryable: true,
      });
      return;
    }

    await fulfillRailSuccess(route, [createProduct("retry-success-product", "Retry Success Product")]);
  });

  await gotoComponentMvp(page);
  await page.waitForFunction(() => {
    const element = document.querySelector("[data-storefront-product-rail-error]");
    return element?.getAttribute("data-storefront-error-code") === "component_mvp_rail_outage";
  }, null, { timeout: 60000 });
  const errorCode = await page.locator("[data-storefront-product-rail-error]").getAttribute("data-storefront-error-code");
  assert(errorCode === "component_mvp_rail_outage", `unexpected rail error code '${errorCode}'`);
  await page.locator("[data-storefront-product-rail-retry]").click();
  await page.waitForSelector("[data-storefront-product-rail-item]", { timeout: 30000 });
  const count = await page.locator("[data-storefront-product-rail-item]").count();
  assert(count === 1, `retry rail expected 1 item, got ${count}`);
  assert(attempts === 2, `retry rail expected 2 BFF attempts, got ${attempts}`);
  steps.push({ step: "component-mvp.rail-error-and-retry-state", ok: true, attempts });
  await recordStorageCredentialLeaks(page);
  await page.close();
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
  await recordStorageCredentialLeaks(page);
}

async function gotoComponentMvp(page) {
  const response = await page.goto(`${baseUrl}/__qa/component-mvp`, {
    waitUntil: "domcontentloaded",
    timeout: 60000,
  });
  assert(response && response.status() === 200, `/__qa/component-mvp returned HTTP ${response ? response.status() : "no response"}`);
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
    await fulfillRailSuccess(route, [createProduct("component-mvp-product", "Component MVP Product")]);
  });
}

async function fulfillRailSuccess(route, products) {
  await fulfillJson(route, { products, success: true, retryable: false });
}

async function fulfillJson(route, payload, status = 200) {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(payload),
  });
}

function createProduct(slug, name) {
  return {
    id: productIdFromSlug(slug),
    name,
    productUrl: `/product/${slug}`,
    categoryName: "QA",
    categoryUrl: "/category/qa",
    imageUrl: "",
    description: `Deterministic ${name}.`,
    priceDisplay: "USD 10.00",
    comparePriceDisplay: "USD 20.00",
    hasVariants: false,
    inStock: true,
    isNewArrival: false,
    purchasable: true,
    purchaseUrl: `/product/${slug}#purchase`,
    canAddDirectly: false,
    unitPriceValue: "10.00",
    currencyCode: "USD",
    directAddStockValue: 1,
    purchaseBlockMessage: null,
    purchasePaused: false,
  };
}

function productIdFromSlug(slug) {
  const suffix = Math.abs(hashCode(slug)).toString().padStart(12, "0").slice(0, 12);
  return `11111111-1111-4111-8111-${suffix}`;
}

function hashCode(value) {
  let hash = 0;
  for (let index = 0; index < value.length; index += 1) {
    hash = ((hash << 5) - hash + value.charCodeAt(index)) | 0;
  }

  return hash;
}

function attachRuntimeGuards(page) {
  page.on("request", (request) => {
    const url = new URL(request.url());
    recordHeaderCredentialLeaks(request);
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

function recordHeaderCredentialLeaks(request) {
  const headers = request.headers();
  for (const [key, value] of Object.entries(headers)) {
    if (isNodeCredentialText(key) || isNodeCredentialText(value)) {
      credentialLeaks.push(`header ${request.method()} ${request.url()} ${key}`);
    }
  }
}

async function recordStorageCredentialLeaks(page) {
  const leaks = await page.evaluate(() => {
    const values = [];
    for (const storage of [window.localStorage, window.sessionStorage]) {
      for (let index = 0; index < storage.length; index += 1) {
        const key = storage.key(index) || "";
        values.push(`${key}=${storage.getItem(key) || ""}`);
      }
    }

    return values;
  });

  for (const value of leaks) {
    if (isNodeCredentialText(value)) {
      credentialLeaks.push(`storage ${value}`);
    }
  }
}

function isNodeCredentialText(value) {
  return /commerce[-_]?node|node[-_]?credential|node[-_]?secret|x[-_]?node|api[-_]?key/i.test(String(value || ""));
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
  assert(credentialLeaks.length === 0, `node credential leaks detected: ${credentialLeaks.join(", ")}`);
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
    credentialLeaks,
  };
  fs.mkdirSync(artifactRoot, { recursive: true });
  fs.writeFileSync(path.join(artifactRoot, `${phase}.evidence.failed.json`), JSON.stringify(failure, null, 2));
  console.error(error);
  process.exit(1);
});
