#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { chromium } from "@playwright/test";

const baseUrl = readArg("--base-url") ?? "http://127.0.0.1:18991";
const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const reportPath = `${projectRoot}/docs/storefront-analysis/functional-commerce-report.md`;
const directCommerceCalls = [];
const checks = [];
const notes = [];

const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  page.on("request", (request) => {
    const url = request.url();
    if (url.includes("/api/storefront/") || url.includes("/api/commerce/")) {
      directCommerceCalls.push(url);
    }
  });

  await checkRoute(page, "/", "Home renders");
  await checkRoute(page, "/category/sample-category", "Catalog renders");
  await checkRoute(page, "/product/sample-product", "Product renders");
  await checkRoute(page, "/cart", "Cart page renders");
  await checkRoute(page, "/checkout", "Checkout route renders");
  await checkRoute(page, "/account", "Account route renders");
  await checkRoute(page, "/signin", "Login/register shell renders according to store policy");

  await page.goto(new URL("/", baseUrl).toString());
  let productLink = page.locator('a[href^="/product/"]').first();
  if ((await productLink.count()) === 0) {
    await page.goto(new URL("/category/sample-category", baseUrl).toString());
    productLink = page.locator('a[href^="/product/"]').first();
  }

  if ((await productLink.count()) > 0) {
    await productLink.click();
    await page.waitForLoadState("networkidle");
    checks.push("Product link navigation works");
  } else if (checks.includes("Product renders")) {
    checks.push("Product route renders without catalog link");
    notes.push("No product link was present on home or catalog, so navigation click proof is not available in this fixture.");
  }

  await page.goto(new URL("/product/sample-product", baseUrl).toString());
  await page.locator(".sfb-product-gallery").waitFor();
  checks.push("Product image/gallery region renders");

  const quantity = page.locator(".sfb-quantity-control input").first();
  await quantity.fill("2");
  if ((await quantity.inputValue()) === "2") {
    checks.push("Quantity control can change");
  }

  const addToCart = page.locator('[data-action="cart.add-line"]').first();
  if ((await addToCart.count()) === 1) {
    const disabled = await addToCart.isDisabled();
    if (disabled) {
      checks.push("Add-to-cart has explicit placeholder or observable result");
      notes.push("Add-to-cart is present but disabled, so this run is selector/placeholder smoke rather than commerce command proof.");
    } else {
      const cartBadge = page.locator(".sfb-cart-badge span").first();
      const beforeBadge = (await cartBadge.count()) === 1 ? await cartBadge.innerText() : "";
      await addToCart.click();
      await page.waitForLoadState("networkidle");
      const afterBadge = (await cartBadge.count()) === 1 ? await cartBadge.innerText() : "";
      const successOrBadgeChange = beforeBadge !== afterBadge || (await page.locator('[role="status"], .starter-toast-region, .starter-alert').count()) > 0;
      if (successOrBadgeChange) {
        checks.push("Add-to-cart has explicit placeholder or observable result");
        checks.push("Add-to-cart command produces an observable cart result");
      }
    }
  }

  const html = await page.content();
  if (html.includes("application/ld+json") && html.includes("canonical")) {
    checks.push("Product SEO initial HTML exists");
  }

  if (directCommerceCalls.length === 0) {
    checks.push("Browser does not call Commerce Node protected APIs directly");
  }
} finally {
  await browser.close();
}

const required = [
  "Home renders",
  "Catalog renders",
  "Product renders",
  "Product link navigation works or explicit fixture gap is reported",
  "Product image/gallery region renders",
  "Quantity control can change",
  "Add-to-cart has explicit placeholder or observable result",
  "Cart page renders",
  "Checkout route renders",
  "Account route renders",
  "Login/register shell renders according to store policy",
  "Product SEO initial HTML exists",
  "Browser does not call Commerce Node protected APIs directly",
];
const missing = required.filter((item) => !isCheckSatisfied(item));
const report = [
  "# StorefrontBuilder Functional Commerce Smoke Report",
  "",
  `Base URL: ${baseUrl}`,
  "Commerce command proof: selector/placeholder smoke unless an enabled command produces an observable cart result.",
  "",
  "## Checks",
  "",
  ...required.map((item) => `- ${isCheckSatisfied(item) ? "[x]" : "[ ]"} ${item}`),
  "",
  "## Browser Network Guard",
  "",
  directCommerceCalls.length === 0 ? "- No direct Commerce Node browser calls detected." : `- Direct Commerce calls detected: ${directCommerceCalls.join(", ")}`,
  "",
  "## Payment Notes",
  "",
  "- COD order placement requires a configured test store/env.",
  "- PayPal/Stripe production providers are outside this MVP gate.",
  "",
  "## Notes",
  "",
  ...(notes.length === 0 ? ["- None."] : notes.map((note) => `- ${note}`)),
  "",
].join("\n");

mkdirSync(dirname(reportPath), { recursive: true });
writeFileSync(reportPath, report, "utf8");
console.log(`Functional commerce report written to ${reportPath}`);
if (missing.length > 0 || directCommerceCalls.length > 0) {
  process.exitCode = 1;
}

async function checkRoute(page, route, label) {
  await page.goto(new URL(route, baseUrl).toString(), { waitUntil: "networkidle" });
  await page.locator("body").waitFor();
  checks.push(label);
}

function isCheckSatisfied(item) {
  if (item === "Product link navigation works or explicit fixture gap is reported") {
    return checks.includes("Product link navigation works") || checks.includes("Product route renders without catalog link");
  }

  return checks.includes(item);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
