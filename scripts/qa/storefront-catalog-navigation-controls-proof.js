const fs = require("fs");
const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const baseUrl = trimEnd(process.env.STOREFRONT_BASE_URL || "http://localhost:18598", "/");
const artifactRoot = path.resolve(__dirname, "../../output/playwright/storefront-catalog-navigation-controls");
const directCommerceCalls = [];
const serverUiCircuitCalls = [];
const consoleErrors = [];
const pageErrors = [];
const steps = [];

async function main() {
  fs.mkdirSync(artifactRoot, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  attachGuards(page);

  try {
    await verifyCategoryFilterAndPagination(page);
    await verifySearchFilterAndPagination(page);
    await verifyBreadcrumbs(page);

    assert(directCommerceCalls.length === 0, `direct Commerce browser calls detected: ${directCommerceCalls.join(", ")}`);
    assert(serverUiCircuitCalls.length === 0, `public Blazor Server circuit requests detected: ${serverUiCircuitCalls.join(", ")}`);
    assert(consoleErrors.length === 0, `console errors detected: ${consoleErrors.join(" | ")}`);
    assert(pageErrors.length === 0, `page errors detected: ${pageErrors.join(" | ")}`);

    const evidence = {
      ok: true,
      baseUrl,
      generatedAtUtc: new Date().toISOString(),
      steps,
      directCommerceCalls,
      serverUiCircuitCalls,
      consoleErrors,
      pageErrors,
    };

    fs.writeFileSync(path.join(artifactRoot, "evidence.json"), JSON.stringify(evidence, null, 2));
    console.log(JSON.stringify(evidence, null, 2));
  } finally {
    await context.close();
    await browser.close();
  }
}

async function verifyCategoryFilterAndPagination(page) {
  const response = await page.goto(`${baseUrl}/category/t-shirts`, { waitUntil: "networkidle", timeout: 60000 });
  assert(response?.ok(), `category page returned ${response?.status() ?? "no response"}`);

  const form = page.locator('form:has(input[name="minPrice"])').first();
  await form.locator('input[name="minPrice"]').fill("10");
  await form.locator('input[name="maxPrice"]').fill("130");
  await form.locator('select[name="sortBy"]').selectOption("priceHighToLow");
  await form.locator('select[name="pageSize"]').selectOption("12");
  await form.locator('input[name="inStock"]').check();
  await submitGetForm(page, form, (url) =>
    url.pathname === "/category/t-shirts" &&
    url.searchParams.get("minPrice") === "10" &&
    url.searchParams.get("maxPrice") === "130" &&
    url.searchParams.get("sortBy") === "priceHighToLow" &&
    url.searchParams.get("pageSize") === "12" &&
    url.searchParams.get("inStock") === "true");

  const paginationResponse = await page.goto(
    `${baseUrl}/category/t-shirts?minPrice=10&maxPrice=130&sortBy=priceHighToLow&pageSize=12`,
    { waitUntil: "networkidle", timeout: 60000 });
  assert(paginationResponse?.ok(), `category pagination page returned ${paginationResponse?.status() ?? "no response"}`);

  await assertPaginationQueryPreserved(page, "Category product pages", {
    minPrice: "10",
    maxPrice: "130",
    sortBy: "priceHighToLow",
    pageSize: "12",
  });
  await screenshot(page, "category-filtered-pagination.png");
  steps.push({ step: "category.filter-and-pagination-query-preservation", ok: true });
}

async function verifySearchFilterAndPagination(page) {
  const response = await page.goto(`${baseUrl}/search`, { waitUntil: "networkidle", timeout: 60000 });
  assert(response?.ok(), `search page returned ${response?.status() ?? "no response"}`);

  const form = page.locator('form[role="search"]:has(input[name="minPrice"])').first();
  await form.locator('select[name="category"]').selectOption("t-shirts");
  await form.locator('input[name="q"]').fill("qa");
  await form.locator('input[name="minPrice"]').fill("10");
  await form.locator('input[name="maxPrice"]').fill("130");
  await form.locator('select[name="sortBy"]').selectOption("priceLowToHigh");
  await form.locator('select[name="pageSize"]').selectOption("12");
  await form.locator('input[name="inStock"]').check();
  await submitGetForm(page, form, (url) =>
    url.pathname === "/search" &&
    url.searchParams.get("category") === "t-shirts" &&
    url.searchParams.get("q") === "qa" &&
    url.searchParams.get("minPrice") === "10" &&
    url.searchParams.get("maxPrice") === "130" &&
    url.searchParams.get("sortBy") === "priceLowToHigh" &&
    url.searchParams.get("pageSize") === "12" &&
    url.searchParams.get("inStock") === "true");
  steps.push({ step: "search.filter-query-preservation", ok: true });

  const paginationResponse = await page.goto(
    `${baseUrl}/search?minPrice=10&maxPrice=130&sortBy=priceLowToHigh&pageSize=12`,
    { waitUntil: "networkidle", timeout: 60000 });
  assert(paginationResponse?.ok(), `filtered search page returned ${paginationResponse?.status() ?? "no response"}`);

  await assertPaginationQueryPreserved(page, "Search result pages", {
    minPrice: "10",
    maxPrice: "130",
    sortBy: "priceLowToHigh",
    pageSize: "12",
  });
  await screenshot(page, "search-filtered-pagination.png");
  steps.push({ step: "search.pagination-query-preservation", ok: true });
}

async function assertPaginationQueryPreserved(page, ariaLabel, expectedQuery) {
  const pagination = page.locator(`nav[aria-label="${ariaLabel}"]`);
  await pagination.waitFor({ state: "visible", timeout: 30000 });
  const links = pagination.locator("a");
  assert(await links.count() >= 2, `${ariaLabel} did not render more than one page link`);

  await Promise.all([
    page.waitForURL((url) => url.searchParams.get("page") === "2", { timeout: 30000 }),
    links.nth(1).click(),
  ]);

  const currentUrl = new URL(page.url());
  assert(currentUrl.searchParams.get("page") === "2", `${ariaLabel} did not move to page 2`);
  for (const [name, value] of Object.entries(expectedQuery)) {
    assert(currentUrl.searchParams.get(name) === value, `${ariaLabel} dropped ${name} from page 2 URL`);
  }
}

async function verifyBreadcrumbs(page) {
  for (const route of ["/category/t-shirts", "/product/catalog-qa-t-shirt", "/pages/faq"]) {
    const response = await page.goto(`${baseUrl}${route}`, { waitUntil: "networkidle", timeout: 60000 });
    assert(response?.ok(), `${route} returned ${response?.status() ?? "no response"}`);
    await page.locator('nav[aria-label="Breadcrumb"]').waitFor({ state: "visible", timeout: 30000 });
  }

  await screenshot(page, "content-breadcrumb.png");
  steps.push({ step: "category-product-content-breadcrumbs", ok: true });
}

async function submitGetForm(page, form, predicate) {
  await Promise.all([
    page.waitForURL(predicate, { timeout: 30000 }),
    form.evaluate((element) => element.requestSubmit()),
  ]);
}

function attachGuards(page) {
  page.on("request", (request) => {
    const url = new URL(request.url());
    if (url.origin === "http://localhost:5180" || url.pathname.startsWith("/api/storefront/") || url.pathname.startsWith("/api/commerce/")) {
      directCommerceCalls.push(`${request.method()} ${url.href}`);
    }

    if (url.pathname === "/_blazor") {
      serverUiCircuitCalls.push(`${request.method()} ${url.href}`);
    }
  });

  page.on("console", (message) => {
    if (message.type() === "error") {
      consoleErrors.push(message.text());
    }
  });

  page.on("pageerror", (error) => pageErrors.push(error.message));
}

async function screenshot(page, fileName) {
  await page.screenshot({ path: path.join(artifactRoot, fileName), fullPage: true });
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function trimEnd(value, suffix) {
  return value.endsWith(suffix) ? value.slice(0, -suffix.length) : value;
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
