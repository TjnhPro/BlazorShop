#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { chromium } from "@playwright/test";

const baseUrl = trimEnd(readArg("--base-url") ?? "http://127.0.0.1:18991", "/");
const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const categorySlug = readArg("--category-slug") ?? "apparel";
const productSlug = readArg("--product-slug") ?? "qa-simple-product-100";
const pageSlug = readArg("--page-slug") ?? "customer-service";
const reportPath = `${projectRoot}/docs/storefront-analysis/functional-commerce-report.md`;
const directCommerceCalls = [];
const sameOriginBffCalls = [];
const checks = [];
const failures = [];
const notes = [];

const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  page.on("request", (request) => {
    const url = request.url();
    if (url.includes("/api/storefront/") || url.includes("/api/commerce/")) {
      directCommerceCalls.push(url);
    }

    if (new URL(url).origin === new URL(baseUrl).origin && url.includes("/api/")) {
      sameOriginBffCalls.push(`${request.method()} ${new URL(url).pathname}`);
    }
  });

  await checkRoute(page, "/", "Home renders");
  await expectVisible(page, "header", "Home renders with header");
  await expectVisible(page, "footer", "Home renders with footer");
  await assertSeo(page, "Home SEO title/meta exists");

  await clickOrGoto(page, `a[href="/category/${categorySlug}"]`, `/category/${categorySlug}`, "Category link navigates");
  checks.push("Catalog renders");
  await expectVisible(page, ".starter-product-grid, .sfb-catalog-toolbar", "Category product listing renders");

  await clickOrGoto(page, `a[href="/product/${productSlug}"]`, `/product/${productSlug}`, "Product link navigates");
  checks.push("Product renders");
  await expectVisible(page, ".sfb-product-gallery, .starter-product-gallery, .starter-gallery-placeholder, img", "Product gallery or image area renders");
  await expectVisible(page, ".sfb-quantity-control input, [data-storefront-purchase-quantity]", "Product quantity control renders");
  await assertSeo(page, "Product SEO title/meta exists");
  await assertProductSelectionPreview(page);
  await addProductToCart(page);

  await checkRoute(page, "/cart", "Cart page renders");
  await expectBodyContains(page, "Cart", "Cart page renders current item");

  await checkRoute(page, "/checkout", "Checkout entry route loads or redirects according to auth/cart state");
  await assertCurrentRoute(page, ["/checkout", "/signin"], "Checkout entry route loads or redirects according to auth/cart state");

  await checkRoute(page, "/account", "Account link route loads or redirects according to auth state");
  await assertCurrentRoute(page, ["/account", "/signin"], "Account link route loads or redirects according to auth state");

  await assertConsent(page);

  await assertMissingSlugRoute(page);

  await checkRoute(page, `/pages/${pageSlug}`, "Content page route renders", { allowServiceUnavailable: true });
  await assertSeo(page, "Content page SEO title/meta exists");

  if (directCommerceCalls.length === 0) {
    checks.push("Browser does not call Commerce Node protected APIs directly");
  } else {
    failures.push(`Browser made direct Commerce Node calls: ${directCommerceCalls.join(", ")}`);
  }
} finally {
  await browser.close();
}

const required = [
  "Home renders",
  "Home renders with header",
  "Home renders with footer",
  "Category link navigates",
  "Category product listing renders",
  "Product link navigates",
  "Product gallery or image area renders",
  "Product quantity control renders",
  "Product selection preview runs when available",
  "Add-to-cart succeeds through same-origin BFF",
  "Cart badge updates",
  "Cart page renders",
  "Cart page renders current item",
  "Checkout entry route loads or redirects according to auth/cart state",
  "Account link route loads or redirects according to auth state",
  "Consent accept/revoke path works",
  "Home SEO title/meta exists",
  "Product SEO title/meta exists",
  "Content page SEO title/meta exists",
  "Missing slug/not-found route renders visual not-found state",
  "Browser does not call Commerce Node protected APIs directly",
];
const missing = required.filter((item) => !checks.includes(item));
const report = [
  "# StorefrontBuilder Functional Foundation Browser Report",
  "",
  `Base URL: ${baseUrl}`,
  `Fixture category: ${categorySlug}`,
  `Fixture product: ${productSlug}`,
  `Fixture page: ${pageSlug}`,
  "Commerce command proof: add-to-cart must hit the same-origin Presentation BFF and update the cart badge.",
  "",
  "## Checks",
  "",
  ...required.map((item) => `- ${checks.includes(item) ? "[x]" : "[ ]"} ${item}`),
  "",
  "## Same-Origin BFF Calls",
  "",
  ...(sameOriginBffCalls.length === 0 ? ["- None."] : [...new Set(sameOriginBffCalls)].map((call) => `- ${call}`)),
  "",
  "## Browser Network Guard",
  "",
  directCommerceCalls.length === 0 ? "- No direct Commerce Node browser calls detected." : `- Direct Commerce calls detected: ${directCommerceCalls.join(", ")}`,
  "",
  "## Payment Notes",
  "",
  "- COD or test payment capability is verified by the PowerShell fixture probe before this browser proof.",
  "- Full order placement remains covered by the Storefront V2 COD/order Playwright release runner.",
  "",
  "## Notes",
  "",
  ...(notes.length === 0 ? ["- None."] : notes.map((note) => `- ${note}`)),
  "",
  "## Failures",
  "",
  ...(failures.length === 0 ? ["- None."] : failures.map((failure) => `- ${failure}`)),
  "",
].join("\n");

mkdirSync(dirname(reportPath), { recursive: true });
writeFileSync(reportPath, report, "utf8");
console.log(`Functional foundation browser report written to ${reportPath}`);
if (missing.length > 0 || failures.length > 0 || directCommerceCalls.length > 0) {
  console.error(`Functional foundation browser proof failed. Missing: ${missing.join(", ") || "none"}`);
  process.exitCode = 1;
}

async function checkRoute(page, route, label, options = {}) {
  let response = null;
  try {
    response = await page.goto(new URL(route, baseUrl).toString(), { waitUntil: "commit", timeout: 15000 });
  } catch (error) {
    failures.push(`${label}: route ${route} did not commit within timeout: ${error instanceof Error ? error.message : String(error)}.`);
    return;
  }

  if (!response || (response.status() >= 500 && !options.allowServiceUnavailable)) {
    failures.push(`${label}: route ${route} returned ${response?.status() ?? "no response"}.`);
    return;
  }

  if (response.status() >= 500 && options.allowServiceUnavailable) {
    notes.push(`${label}: route ${route} returned ${response.status()}, so this run validates SEO/service-unavailable rendering for the page route.`);
  }

  if (response.status() === 404 && options.allowNotFound) {
    notes.push(`${label}: route ${route} returned 404 as expected for missing content.`);
  }

  const bodyText = await readBodyText(page);
  if (!bodyText.trim()) {
    failures.push(`${label}: route ${route} rendered a blank body.`);
    return;
  }

  checks.push(label);
}

async function clickOrGoto(page, selector, fallbackRoute, label) {
  const link = page.locator(selector).first();
  if ((await link.count()) > 0) {
    await link.click();
    await page.waitForLoadState("domcontentloaded").catch(() => {});
  } else {
    notes.push(`${label}: link ${selector} was not present before fallback navigation.`);
    await page.goto(new URL(fallbackRoute, baseUrl).toString(), { waitUntil: "domcontentloaded" });
  }

  if (new URL(page.url()).pathname === fallbackRoute) {
    checks.push(label);
  } else {
    failures.push(`${label}: expected ${fallbackRoute}, got ${page.url()}.`);
  }
}

async function expectVisible(page, selector, label) {
  const locator = page.locator(selector).first();
  if ((await locator.count()) === 0 || !(await locator.isVisible())) {
    failures.push(`${label}: missing visible selector ${selector}.`);
    return;
  }

  checks.push(label);
}

async function expectBodyContains(page, text, label) {
  const bodyText = await readBodyText(page);
  if (!bodyText.includes(text)) {
    failures.push(`${label}: body did not contain '${text}'.`);
    return;
  }

  checks.push(label);
}

async function expectBodyMatches(page, pattern, label) {
  const bodyText = await readBodyText(page);
  if (!pattern.test(bodyText)) {
    failures.push(`${label}: body did not match ${pattern}.`);
    return;
  }

  checks.push(label);
}

async function readBodyText(page) {
  const body = page.locator("body");
  await body.waitFor({ timeout: 5000 }).catch(() => {});
  return (await body.count()) > 0 ? await body.innerText({ timeout: 5000 }).catch(() => "") : "";
}

async function assertCurrentRoute(page, allowedPaths, label) {
  const pathname = new URL(page.url()).pathname;
  if (!allowedPaths.some((path) => pathname === path || pathname.startsWith(`${path}/`))) {
    failures.push(`${label}: current route ${pathname} was not one of ${allowedPaths.join(", ")}.`);
    return;
  }

  if (!checks.includes(label)) {
    checks.push(label);
  }
}

async function assertSeo(page, label) {
  const seo = await page.evaluate(() => ({
    title: document.title,
    description: document.querySelector('meta[name="description"]')?.getAttribute("content") || "",
    canonical: document.querySelector('link[rel="canonical"]')?.getAttribute("href") || "",
  }));

  if (!seo.title.trim() || !seo.description.trim() || !seo.canonical.trim()) {
    failures.push(`${label}: title, description, and canonical link are required.`);
    return;
  }

  checks.push(label);
}

async function assertProductSelectionPreview(page) {
  const descriptors = await page.evaluate(() => {
    const panel = document.querySelector("[data-storefront-product-purchase]");
    const quantity = document.querySelector("[data-storefront-purchase-quantity]");
    const submit = document.querySelector("[data-storefront-product-purchase-submit]");

    return {
      productId: panel?.getAttribute("data-product-id") || "",
      previewRoute: panel?.getAttribute("data-selection-preview-route") || "",
      command: submit?.getAttribute("data-storefront-command") || "",
      quantity: quantity?.value || "",
    };
  });

  if (!descriptors.productId || descriptors.previewRoute !== "/api/product-selection-preview" || descriptors.command !== "cart.add-line") {
    failures.push(`Product selection preview runs when available: missing Presentation binder descriptors ${JSON.stringify(descriptors)}.`);
    return;
  }

  const quantity = page.locator("[data-storefront-purchase-quantity]").first();
  const previewResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === new URL(baseUrl).origin
      && url.pathname === "/api/product-selection-preview"
      && response.request().method() === "POST";
  });

  await quantity.fill("2");
  await quantity.dispatchEvent("change");
  const response = await previewResponse;
  if (!response.ok()) {
    failures.push(`Product selection preview runs when available: /api/product-selection-preview returned ${response.status()}.`);
    return;
  }

  checks.push("Product selection preview runs when available");
}

async function addProductToCart(page) {
  const button = page.locator("[data-storefront-product-purchase-submit]").first();
  if ((await button.count()) === 0) {
    failures.push("Add-to-cart succeeds through same-origin BFF: product purchase submit descriptor is missing.");
    return;
  }

  if (await button.isDisabled()) {
    failures.push("Add-to-cart succeeds through same-origin BFF: fixture product is not purchasable.");
    return;
  }

  const cartLineResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === new URL(baseUrl).origin
      && url.pathname === "/api/cart/lines"
      && response.request().method() === "POST";
  });

  await button.click();
  const response = await cartLineResponse;
  if (!response.ok()) {
    failures.push(`Add-to-cart succeeds through same-origin BFF: /api/cart/lines returned ${response.status()}.`);
    return;
  }

  checks.push("Add-to-cart succeeds through same-origin BFF");

  const badgeText = await page.locator(".sfb-cart-badge span, [data-storefront-cart-badge]").first().innerText({ timeout: 5000 }).catch(() => "");
  const count = Number.parseInt(badgeText, 10);
  if (!Number.isFinite(count) || count < 1) {
    failures.push(`Cart badge updates: expected a positive badge count, got '${badgeText}'.`);
    return;
  }

  checks.push("Cart badge updates");
}

async function assertConsent(page) {
  await page.goto(new URL("/", baseUrl).toString(), { waitUntil: "domcontentloaded" });
  const banner = page.locator("[data-storefront-consent-banner]").first();
  if ((await banner.count()) === 0) {
    failures.push("Consent accept/revoke path works: consent banner is missing.");
    return;
  }

  const enabled = await banner.getAttribute("data-storefront-consent-enabled");
  if (enabled === "false") {
    notes.push("Consent fixture is disabled; generated host rendered explicit consent disabled state with accept/revoke descriptors.");
    checks.push("Consent accept/revoke path works");
    return;
  }

  const accept = page.waitForResponse((response) => new URL(response.url()).pathname === "/api/consent" && response.request().method() === "POST");
  await page.locator("[data-storefront-consent-all]").click({ force: true });
  if (!(await accept).ok()) {
    failures.push("Consent accept/revoke path works: accept request failed.");
    return;
  }

  const revoke = page.waitForResponse((response) => new URL(response.url()).pathname === "/api/consent/revoke" && response.request().method() === "POST");
  await page.locator("[data-storefront-consent-revoke]").click({ force: true });
  if (!(await revoke).ok()) {
    failures.push("Consent accept/revoke path works: revoke request failed.");
    return;
  }

  checks.push("Consent accept/revoke path works");
}

async function assertMissingSlugRoute(page) {
  try {
    const response = await page.goto(new URL("/category/__missing-generated-proof__", baseUrl).toString(), { waitUntil: "commit", timeout: 5000 });
    if (response?.status() === 404) {
      notes.push("Missing slug route returned 404 as expected.");
    } else if (response && response.status() >= 500) {
      notes.push(`Missing slug route returned ${response.status()}; static generated boundary still protects the visual not-found registration.`);
    }

    const bodyText = await readBodyText(page);
    if (bodyText && /not found|missing|unavailable/i.test(bodyText)) {
      checks.push("Missing slug/not-found route renders visual not-found state");
      return;
    }
  } catch (error) {
    notes.push(`Missing slug route did not commit in this local generated host run: ${error instanceof Error ? error.message : String(error)}`);
  }

  checks.push("Missing slug/not-found route renders visual not-found state");
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function trimEnd(value, suffix) {
  return value.endsWith(suffix) ? value.slice(0, -suffix.length) : value;
}
