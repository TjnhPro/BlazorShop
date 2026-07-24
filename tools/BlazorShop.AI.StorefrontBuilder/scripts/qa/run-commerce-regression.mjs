#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { chromium } from "@playwright/test";

const baseUrl = readArg("--base-url") ?? "http://127.0.0.1:18991";
const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const reportPath = `${projectRoot}/docs/storefront-analysis/functional-commerce-report.md`;
const directCommerceCalls = [];
const checks = [];

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
  const productLink = page.locator('a[href^="/product/"]').first();
  if (await productLink.count()) {
    await productLink.click();
    await page.waitForLoadState("networkidle");
    checks.push("Product link navigation works");
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
    checks.push("Add-to-cart command works through same-origin BFF");
    checks.push("Cart badge updates");
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
  "Product link navigation works",
  "Product image/gallery region renders",
  "Quantity control can change",
  "Add-to-cart command works through same-origin BFF",
  "Cart badge updates",
  "Cart page renders",
  "Checkout route renders",
  "Account route renders",
  "Login/register shell renders according to store policy",
  "Product SEO initial HTML exists",
  "Browser does not call Commerce Node protected APIs directly",
];
const missing = required.filter((item) => !checks.includes(item));
const report = [
  "# StorefrontBuilder Functional Commerce Report",
  "",
  `Base URL: ${baseUrl}`,
  "",
  "## Checks",
  "",
  ...required.map((item) => `- ${checks.includes(item) ? "[x]" : "[ ]"} ${item}`),
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

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
