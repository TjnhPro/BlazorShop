const fs = require("fs");
const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const baseUrl = trimEnd(process.env.STOREFRONT_BASE_URL || "http://localhost:18598", "/");
const artifactRoot = path.resolve(__dirname, "../../output/playwright/storefront-reference-components-phase14");
const directCommerceCalls = [];
const sameOriginCalls = [];
const consoleErrors = [];
const pageErrors = [];
const screenshots = [];
const steps = [];

async function main() {
  fs.mkdirSync(artifactRoot, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });

  try {
    const page = await context.newPage();
    attachGuards(page);

    await assertSsrContactMarkup(page);
    await verifyDesktopHeaderAndRailSuccess(page);
    await verifyContactStates(page);
    await verifyRailEmptyAndRetry(context);
    await verifyMobileHeader(context);

    assert(directCommerceCalls.length === 0, `direct Commerce browser calls detected: ${directCommerceCalls.join(", ")}`);
    assert(pageErrors.length === 0, `page errors detected: ${pageErrors.join(" | ")}`);
    assert(consoleErrors.length === 0, `console errors detected: ${consoleErrors.join(" | ")}`);

    const evidence = {
      ok: true,
      baseUrl,
      generatedAtUtc: new Date().toISOString(),
      viewports: [
        { name: "desktop", width: 1440, height: 1000 },
        { name: "mobile", width: 390, height: 844 },
      ],
      steps,
      sameOriginCalls: [...new Set(sameOriginCalls)],
      directCommerceCalls,
      consoleErrors,
      pageErrors,
      screenshots,
    };

    fs.writeFileSync(path.join(artifactRoot, "evidence.json"), JSON.stringify(evidence, null, 2));
    console.log(JSON.stringify(evidence, null, 2));
  } finally {
    await context.close();
    await browser.close();
  }
}

async function assertSsrContactMarkup(page) {
  const response = await page.request.get(`${baseUrl}/pages/customer-service`, { timeout: 30000 });
  assert(response.ok(), `customer-service SSR request returned ${response.status()}`);
  const html = await response.text();

  assert(html.includes('data-storefront-component="contact-form"'), "SSR shell contact-form component marker missing");
  assert(html.includes("data-storefront-contact-form"), "SSR-first contact form marker missing");
  assert(html.includes('name="Subject"'), "SSR-first contact subject field missing");
  steps.push({ step: "contact.ssr-first-markup", ok: true });
}

async function verifyDesktopHeaderAndRailSuccess(page) {
  const response = await page.goto(`${baseUrl}/`, { waitUntil: "networkidle", timeout: 60000 });
  assert(response && response.ok(), `home returned ${response ? response.status() : "no response"}`);

  await page.waitForSelector('[data-storefront-component="brand-logo"]', { timeout: 15000 });
  await page.waitForSelector("[data-storefront-discounted-product-rail]", { timeout: 30000 });
  await page.waitForSelector("[data-storefront-product-rail-item], [data-storefront-product-rail-empty]", { timeout: 30000 });

  const brand = await page.locator('[data-storefront-component="brand-logo"]').first();
  assert(await brand.isVisible(), "desktop brand logo is not visible");

  const railState = await page.evaluate(() => ({
    items: document.querySelectorAll("[data-storefront-product-rail-item]").length,
    empty: Boolean(document.querySelector("[data-storefront-product-rail-empty]")),
    loading: Boolean(document.querySelector("[data-storefront-product-rail-loading]")),
  }));
  assert(railState.items > 0 || railState.empty, `rail did not reach success/empty state: ${JSON.stringify(railState)}`);

  await screenshot(page, "home-desktop.png");
  steps.push({ step: "home.desktop.brand-and-rail-success", ok: true, railState });

  const contactResponse = await page.goto(`${baseUrl}/pages/customer-service`, { waitUntil: "networkidle", timeout: 60000 });
  assert(contactResponse && contactResponse.ok(), `customer-service returned ${contactResponse ? contactResponse.status() : "no response"}`);
  await page.locator('[data-storefront-component="brand-logo"]').first().click();
  await page.waitForURL(`${baseUrl}/`, { timeout: 15000 });
  steps.push({ step: "header.brand-link-navigates-home", ok: true });
}

async function verifyContactStates(page) {
  const response = await page.goto(`${baseUrl}/pages/customer-service`, { waitUntil: "networkidle", timeout: 60000 });
  assert(response && response.ok(), `customer-service returned ${response ? response.status() : "no response"}`);
  await page.waitForSelector("[data-storefront-contact-form]", { timeout: 30000 });

  await page.locator("[data-storefront-contact-submit]").click();
  const invalidCount = await page.locator("[data-storefront-contact-form] :invalid").count();
  assert(invalidCount >= 4, `expected required contact fields to block empty submit, got ${invalidCount}`);
  steps.push({ step: "contact.browser-required-validation", ok: true, invalidCount });

  await fulfillNextContact(page, {
    status: 200,
    body: {
      success: false,
      defaultMessage: "Validation failed.",
      fieldErrors: { Email: ["Email is required."] },
      retryable: false,
    },
  });
  await fillContact(page, "Validation");
  await submitContactAndWait(page);
  await page.waitForSelector("[data-storefront-contact-error-summary]", { timeout: 15000 });
  steps.push({ step: "contact.validation-failure-state", ok: true });

  await fulfillNextContact(page, {
    status: 200,
    body: {
      success: false,
      defaultMessage: "Simulated backend outage.",
      code: "simulated_contact_outage",
      retryable: true,
    },
  });
  await fillContact(page, "Backend Failure");
  await submitContactAndWait(page);
  await page.waitForSelector("[data-storefront-contact-retry]", { timeout: 15000 });
  steps.push({ step: "contact.backend-failure-and-retry-state", ok: true });

  await fulfillNextContact(page, {
    status: 200,
    body: { success: true, defaultMessage: "Message received." },
  });
  await page.locator("[data-storefront-contact-retry]").click();
  await page.waitForSelector("[data-storefront-contact-status]", { timeout: 15000 });
  const statusText = await page.locator("[data-storefront-contact-status]").innerText();
  assert(/received|success|sent/i.test(statusText), `unexpected contact success text: ${statusText}`);
  await screenshot(page, "contact-success.png");
  steps.push({ step: "contact.success-state", ok: true, statusText });
}

async function verifyRailEmptyAndRetry(context) {
  const emptyPage = await context.newPage();
  attachGuards(emptyPage);
  await emptyPage.route("**/api/catalog/discounted-products?**", async (route) => {
    await fulfillJson(route, { products: [], success: true, retryable: false });
  });
  const emptyResponse = await emptyPage.goto(`${baseUrl}/`, { waitUntil: "networkidle", timeout: 60000 });
  assert(emptyResponse && emptyResponse.ok(), `empty rail home returned ${emptyResponse ? emptyResponse.status() : "no response"}`);
  await emptyPage.waitForSelector("[data-storefront-product-rail-empty]", { timeout: 30000 });
  await screenshot(emptyPage, "rail-empty.png");
  steps.push({ step: "rail.empty-state", ok: true });
  await emptyPage.close();

  const retryPage = await context.newPage();
  attachGuards(retryPage);
  let count = 0;
  await retryPage.route("**/api/catalog/discounted-products?**", async (route) => {
    count += 1;
    if (count === 1) {
      await fulfillJson(route, {
        success: false,
        defaultMessage: "Simulated rail outage.",
        code: "simulated_rail_outage",
        retryable: true,
      });
      return;
    }

    await fulfillJson(route, {
      products: [{
        id: "11111111-1111-4111-8111-111111111111",
        name: "QA Discounted Retry Product",
        productUrl: "/product/catalog-qa-t-shirt",
        categoryName: "QA",
        categoryUrl: "/category/t-shirts",
        imageUrl: "",
        description: "Retry proof product.",
        priceDisplay: "EUR 10.00",
        comparePriceDisplay: "EUR 20.00",
        hasVariants: false,
        inStock: true,
        isNewArrival: false,
        purchasable: true,
        purchaseUrl: "/product/catalog-qa-t-shirt#purchase",
        canAddDirectly: false,
        unitPriceValue: "10.00",
        currencyCode: "EUR",
        directAddStockValue: 1,
        purchaseBlockMessage: null,
        purchasePaused: false,
      }],
      success: true,
      retryable: false,
    });
  });

  const retryResponse = await retryPage.goto(`${baseUrl}/`, { waitUntil: "networkidle", timeout: 60000 });
  assert(retryResponse && retryResponse.ok(), `retry rail home returned ${retryResponse ? retryResponse.status() : "no response"}`);
  await retryPage.waitForSelector("[data-storefront-product-rail-error]", { timeout: 30000 });
  await screenshot(retryPage, "rail-error.png");
  await retryPage.locator("[data-storefront-product-rail-retry]").click();
  await retryPage.waitForSelector("[data-storefront-product-rail-item]", { timeout: 30000 });
  steps.push({ step: "rail.error-and-retry-state", ok: true, attempts: count });
  await retryPage.close();
}

async function verifyMobileHeader(context) {
  const page = await context.newPage();
  attachGuards(page);
  await page.setViewportSize({ width: 390, height: 844 });
  const response = await page.goto(`${baseUrl}/`, { waitUntil: "networkidle", timeout: 60000 });
  assert(response && response.ok(), `mobile home returned ${response ? response.status() : "no response"}`);
  await page.waitForSelector('[data-storefront-component="brand-logo"]', { state: "attached", timeout: 15000 });
  await page.waitForFunction(() =>
    Array.from(document.querySelectorAll('[data-storefront-component="brand-logo"]')).some((node) => {
      const element = node;
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    }), null, { timeout: 15000 });
  const visibleBrands = await page.locator('[data-storefront-component="brand-logo"]').evaluateAll((nodes) =>
    nodes.filter((node) => {
      const element = node;
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    }).length);
  assert(visibleBrands > 0, "mobile brand logo is not visible");
  await screenshot(page, "home-mobile.png");
  steps.push({ step: "home.mobile.brand-visible", ok: true, visibleBrands });
  await page.close();
}

async function fillContact(page, suffix) {
  await page.locator('[data-storefront-contact-field="name"] input').fill(`QA ${suffix}`);
  await page.locator('[data-storefront-contact-field="email"] input').fill(`qa-${Date.now()}@example.com`);
  await page.locator('[data-storefront-contact-field="subject"] input').fill(`Reference component ${suffix}`);
  await page.locator('[data-storefront-contact-field="message"] textarea').fill(`Reference component ${suffix} browser proof.`);
}

async function submitContactAndWait(page, options = {}) {
  const expectedStatus = options.expectedStatus || 200;
  const responsePromise = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/contact" && response.request().method() === "POST";
  }, { timeout: 30000 });
  await page.locator("[data-storefront-contact-submit]").click();
  const response = await responsePromise;
  assert(response.status() === expectedStatus, `/api/contact returned ${response.status()}, expected ${expectedStatus}`);
  const headers = response.request().headers();
  const hasAntiforgery = Object.keys(headers).some((key) =>
    key.toLowerCase().includes("requestverificationtoken") ||
    key.toLowerCase().includes("csrf") ||
    key.toLowerCase().includes("xsrf"));
  assert(hasAntiforgery, "contact submit did not include an antiforgery header");
}

async function fulfillNextContact(page, response) {
  let handled = false;
  await page.route("**/api/contact", async (route) => {
    if (handled) {
      await route.fallback();
      return;
    }

    handled = true;
    await fulfillJson(route, response.body, response.status);
  });
}

async function fulfillJson(route, payload, status = 200) {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(payload),
  });
}

async function screenshot(page, name) {
  const filePath = path.join(artifactRoot, name);
  await page.screenshot({ path: filePath, fullPage: true });
  screenshots.push(path.relative(path.resolve(__dirname, "../.."), filePath).replace(/\\/g, "/"));
}

function attachGuards(page) {
  page.on("request", (request) => {
    const url = new URL(request.url());
    if (url.href.includes("/api/storefront/") || url.href.includes("/api/commerce/") || url.origin === "http://localhost:5180") {
      directCommerceCalls.push(`${request.method()} ${url.href}`);
    }

    if (url.origin === baseUrl && url.pathname.startsWith("/api/")) {
      sameOriginCalls.push(`${request.method()} ${url.pathname}`);
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

function trimEnd(value, suffix) {
  return value.endsWith(suffix) ? value.slice(0, -suffix.length) : value;
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

main().catch((error) => {
  const failure = {
    ok: false,
    error: error.stack || String(error),
    steps,
    sameOriginCalls: [...new Set(sameOriginCalls)],
    directCommerceCalls,
    consoleErrors,
    pageErrors,
    screenshots,
  };
  fs.mkdirSync(artifactRoot, { recursive: true });
  fs.writeFileSync(path.join(artifactRoot, "evidence.failed.json"), JSON.stringify(failure, null, 2));
  console.error(error);
  process.exit(1);
});
