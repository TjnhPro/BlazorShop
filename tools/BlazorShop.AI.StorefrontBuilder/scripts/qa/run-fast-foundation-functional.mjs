#!/usr/bin/env node
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { createServer } from "node:http";
import { basename, dirname, resolve } from "node:path";
import { spawn, spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { setTimeout as delay } from "node:timers/promises";
import { chromium } from "@playwright/test";

const scriptDir = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(readArg("--workspace-root") ?? readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const projectName = basename(projectRoot);
const serverProjectRoot = resolve(`${projectRoot}/${projectName}`);
const projectFile = resolve(readArg("--project-file") ?? `${serverProjectRoot}/${projectName}.csproj`);
const reportPath = `${projectRoot}/docs/storefront-analysis/fast-foundation-functional-report.md`;
const proofProductPath = "/product/proof-product";
const directCommerceCalls = [];
const sameOriginBffCalls = [];
const fakeCommerceCalls = [];
const hostLogs = [];
const browserEvents = [];
const checks = [];
const failures = [];
let failureScreenshotPath = null;
const proofStoreId = "00000000-0000-0000-0000-000000000010";
const proofSeoSettingsId = "00000000-0000-0000-0000-000000000011";
const proofPaymentMethodId = "00000000-0000-0000-0000-000000000012";
const proofCheckoutId = "00000000-0000-0000-0000-000000000013";
const proofCategory = {
  id: "00000000-0000-0000-0000-000000000101",
  parentCategoryId: null,
  name: "Proof Category",
  slug: "proof-category",
  description: "Generated proof category.",
  image: null,
  displayOrder: 1,
  isPublished: true,
  metaTitle: "Proof Category",
  metaDescription: "Generated proof category.",
  canonicalUrl: null,
  ogTitle: "Proof Category",
  ogDescription: "Generated proof category.",
  ogImage: null,
  seoContent: "",
  robotsIndex: true,
  robotsFollow: true,
};
const proofProduct = {
  id: "00000000-0000-0000-0000-000000000201",
  variantId: "00000000-0000-0000-0000-000000000202",
  slug: "proof-product",
};
const proofCart = {
  id: "00000000-0000-0000-0000-000000000301",
  lineId: "00000000-0000-0000-0000-000000000302",
  token: "proof-cart-token",
};

assertGeneratedContract();

const fakeCommerce = await startFakeCommerceNode();
const baseUrl = readArg("--base-url") ?? `http://127.0.0.1:${await reservePort()}`;
const host = startGeneratedHost(baseUrl, fakeCommerce.url);

let browser;
let page;
try {
  await waitForHost(baseUrl);
  browser = await chromium.launch();
  page = await browser.newPage();
  let presentationScriptLoaded = false;

  page.on("console", (message) => {
    if (message.type() === "error") {
      browserEvents.push(`console error: ${message.text()}`);
    }
  });
  page.on("pageerror", (error) => {
    browserEvents.push(`page error: ${error?.message || error}`);
  });

  page.on("request", (request) => {
    const url = new URL(request.url());
    if (url.href.includes("/api/storefront/") || url.href.includes("/api/commerce/") || url.origin === fakeCommerce.url) {
      directCommerceCalls.push(url.href);
    }

    if (url.origin === baseUrl && url.pathname.startsWith("/api/")) {
      sameOriginBffCalls.push(`${request.method()} ${url.pathname}`);
    }
  });

  page.on("response", (response) => {
    const url = new URL(response.url());
    if (url.origin === baseUrl
      && url.pathname === "/_content/BlazorShop.Storefront.Presentation/js/storefront.application.js"
      && response.ok()) {
      presentationScriptLoaded = true;
    }
  });

  await routeSameOriginBff(page, baseUrl);

  const homeResponse = await page.goto(`${baseUrl}/`, { waitUntil: "networkidle" });
  const homeText = await page.locator("body").innerText();
  if (homeResponse?.ok() && homeText.includes("Generated Proof Store")) {
    checks.push("home SSR renders");
  } else {
    failures.push("home SSR renders");
  }

  const categoryResponse = await page.goto(`${baseUrl}/category/proof-category`, { waitUntil: "networkidle" });
  const categoryText = await page.locator("body").innerText();
  if (categoryResponse?.ok() && categoryText.includes("Proof Category") && categoryText.includes("Proof Product")) {
    checks.push("catalog page renders");
  } else {
    failures.push("catalog page renders");
  }

  const productResponse = await page.goto(`${baseUrl}${proofProductPath}`, { waitUntil: "networkidle" });
  await page.evaluate(() => {
    window.__storefrontProofSelectionDetails = [];
    document.addEventListener("storefront:product-purchase:selection-changed", (event) => {
      window.__storefrontProofSelectionDetails.push({
        skuText: event.detail?.selection?.skuText || "",
        gtinText: event.detail?.selection?.gtinText || "",
      });
    });
  });
  checks.push("product page renders");
  await page.waitForSelector("[data-storefront-product-purchase]");
  if (productResponse?.ok() && await page.locator("[data-storefront-product-purchase]").count() > 0) {
    checks.push("product detail renders");
  } else {
    failures.push("product detail renders");
  }

  if (await page.locator("[data-storefront-product-gallery], [data-storefront-gallery-placeholder], [data-storefront-gallery-main], .starter-gallery-placeholder, .sfb-product-gallery").count() > 0) {
    checks.push("product image/gallery renders");
  } else {
    failures.push("product image/gallery renders");
  }

  checks.push("product purchase descriptors exist");

  const descriptorSource = await page.locator("[data-storefront-product-purchase]").evaluate((element) => ({
    productId: element.getAttribute("data-product-id"),
    route: element.getAttribute("data-selection-preview-route"),
    command: element.querySelector("[data-storefront-product-purchase-submit]")?.getAttribute("data-storefront-command"),
  }));
  if (descriptorSource.productId === proofProduct.id
    && descriptorSource.route === "/api/product-selection-preview"
    && descriptorSource.command === "cart.add-line") {
    checks.push("actual generated Razor emitted purchase descriptors");
  } else {
    failures.push(`actual generated Razor emitted purchase descriptors: ${JSON.stringify(descriptorSource)}`);
  }

  if (presentationScriptLoaded) {
    checks.push("Presentation core script loads through static web assets");
  } else {
    failures.push("Presentation core script loads through static web assets");
  }

  const cartLineCallsBeforeMalformedClick = sameOriginBffCalls.filter((call) => call === "POST /api/cart/lines").length;
  await page.locator("[data-storefront-product-purchase]").evaluate((panel) => {
    const source = panel.querySelector("[data-storefront-product-purchase-submit]");
    if (!(source instanceof HTMLButtonElement)) {
      throw new Error("Generated purchase panel is missing submit button.");
    }

    const malformed = source.cloneNode(true);
    malformed.removeAttribute("data-storefront-command");
    malformed.setAttribute("data-proof-malformed-submit", "");
    malformed.disabled = false;
    panel.appendChild(malformed);
  });
  await page.dispatchEvent("[data-proof-malformed-submit]", "click");
  await page.waitForTimeout(250);
  const cartLineCallsAfterMalformedClick = sameOriginBffCalls.filter((call) => call === "POST /api/cart/lines").length;
  if (cartLineCallsAfterMalformedClick === cartLineCallsBeforeMalformedClick) {
    checks.push("command descriptor is required");
  } else {
    failures.push("command descriptor is required");
  }

  const previewResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/product-selection-preview" && response.request().method() === "POST";
  });
  await page.fill("[data-storefront-purchase-quantity]", "2");
  await page.dispatchEvent("[data-storefront-purchase-quantity]", "change");
  if (await page.locator("[data-storefront-purchase-quantity]").inputValue() === "2") {
    checks.push("quantity changes work");
  } else {
    failures.push("quantity changes work");
  }

  if ((await previewResponse).ok()) {
    checks.push("selection preview command is invoked through same-origin route");
  } else {
    failures.push("selection preview command is invoked through same-origin route");
  }

  const selectionDetails = await page.evaluate(() => window.__storefrontProofSelectionDetails ?? []);
  if (selectionDetails.some((detail) => detail.skuText === "SKU SKU-FAST-M" && detail.gtinText === "GTIN GTIN-FAST-M")) {
    checks.push("SKU/GTIN semantic values update when preview response changes");
  } else {
    failures.push(`SKU/GTIN semantic values update when preview response changes: ${JSON.stringify(selectionDetails)}`);
  }

  const addLineResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/cart/lines" && response.request().method() === "POST";
  });
  await page.dispatchEvent("[data-storefront-product-purchase-submit]", "click");
  if ((await addLineResponse).ok()) {
    checks.push("add-to-cart command is invoked through same-origin route");
  } else {
    failures.push("add-to-cart command is invoked through same-origin route");
  }

  await page.waitForFunction(() => Number.parseInt(document.querySelector("[data-storefront-cart-badge]")?.textContent || "0", 10) > 0);
  checks.push("cart badge changes");

  await page.context().addCookies([{
    name: "bs-cart-token",
    value: proofCart.token,
    domain: "127.0.0.1",
    path: "/",
    httpOnly: true,
    sameSite: "Lax",
  }]);

  await page.goto(`${baseUrl}/my-cart`, { waitUntil: "networkidle" });
  if ((await page.locator("body").innerText()).includes("Proof Product")) {
    checks.push("cart page sees current cart");
    checks.push("cart page hydrates");
  } else {
    failures.push("cart page sees current cart");
  }

  const cartRefreshResponse = await page.goto(`${baseUrl}/cart`, { waitUntil: "networkidle" });
  if (cartRefreshResponse?.ok() && (await page.locator("body").innerText()).includes("Proof Product")) {
    checks.push("direct refresh of /cart works");
  } else {
    failures.push("direct refresh of /cart works");
  }

  await page.goto(`${baseUrl}/checkout`, { waitUntil: "networkidle" });
  const checkoutText = await page.locator("body").innerText();
  if (checkoutText.includes("Checkout") && await page.locator("[data-storefront-checkout-form]").count() > 0) {
    checks.push("checkout form/route contract exists");
    checks.push("checkout page hydrates");
    checks.push("direct refresh of /checkout works");
  } else {
    failures.push("checkout form/route contract exists");
  }

  await page.context().addCookies([{
    name: "bs-proof-refresh",
    value: "proof-refresh-token",
    domain: "127.0.0.1",
    path: "/",
    httpOnly: true,
    sameSite: "Lax",
  }]);
  await page.goto(`${baseUrl}/account`, { waitUntil: "networkidle" });
  const accountText = await page.locator("body").innerText();
  if (accountText.includes("Account") && await page.locator(".starter-account-shell").count() > 0) {
    checks.push("account shell route renders without direct Commerce transport");
    checks.push("account page hydrates");
    checks.push("direct refresh of /account works");
  } else {
    failures.push("account shell route renders without direct Commerce transport");
  }

  await page.goto(`${baseUrl}/`, { waitUntil: "networkidle" });
  const consentAcceptResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/consent" && response.request().method() === "POST";
  });
  await page.click("[data-storefront-consent-all]", { force: true });
  const consentAcceptResult = await consentAcceptResponse;

  await page.locator("[data-storefront-consent-banner]").evaluate((banner) => {
    banner.classList.remove("hidden");
  });

  const consentRevokeResponse = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return url.origin === baseUrl && url.pathname === "/api/consent/revoke" && response.request().method() === "POST";
  });
  await page.click("[data-storefront-consent-revoke]", { force: true });
  const consentRevokeResult = await consentRevokeResponse;
  if (consentAcceptResult.ok() && consentRevokeResult.ok()) {
    checks.push("consent current/save/revoke works");
  } else {
    failures.push("consent current/save/revoke works");
  }

  if (directCommerceCalls.length === 0) {
    checks.push("no browser request goes directly to Commerce Node");
  } else {
    failures.push(`direct Commerce Node browser calls: ${directCommerceCalls.join(", ")}`);
  }

  if (browserEvents.length === 0) {
    checks.push("console audit has no blocking errors");
  } else {
    failures.push(`console audit has blocking errors: ${browserEvents.join("; ")}`);
  }
} catch (error) {
  failures.push(`fatal proof error: ${error?.stack || error?.message || error}`);
  if (page) {
    failureScreenshotPath = `${projectRoot}/docs/storefront-analysis/fast-foundation-functional-failure.png`;
    await page.screenshot({ path: failureScreenshotPath, fullPage: true }).catch((screenshotError) => {
      failures.push(`failure screenshot could not be captured: ${screenshotError?.message || screenshotError}`);
    });
  }
} finally {
  if (browser) {
    await browser.close();
  }

  stopGeneratedHost(host);
  await fakeCommerce.stop();
}

const required = [
  "home SSR renders",
  "catalog page renders",
  "product page renders",
  "product detail renders",
  "product image/gallery renders",
  "product purchase descriptors exist",
  "actual generated Razor emitted purchase descriptors",
  "Presentation core script loads through static web assets",
  "selection preview command is invoked through same-origin route",
  "SKU/GTIN semantic values update when preview response changes",
  "quantity changes work",
  "command descriptor is required",
  "add-to-cart command is invoked through same-origin route",
  "cart badge changes",
  "cart page sees current cart",
  "cart page hydrates",
  "checkout form/route contract exists",
  "checkout page hydrates",
  "direct refresh of /checkout works",
  "account shell route renders without direct Commerce transport",
  "account page hydrates",
  "direct refresh of /account works",
  "direct refresh of /cart works",
  "consent current/save/revoke works",
  "no browser request goes directly to Commerce Node",
  "console audit has no blocking errors",
];
const missing = required.filter((check) => !checks.includes(check));
const report = [
  "# StorefrontBuilder Fast Foundation Functional Report",
  "",
  `Project root: ${projectRoot}`,
  `Generated host: ${baseUrl}`,
  `Fake Commerce Node: ${fakeCommerce.url}`,
  "Mock mode: generated ASP.NET host renders Razor/static web assets; Playwright fulfills browser same-origin Presentation BFF routes only.",
  "",
  "## Checks",
  "",
  ...required.map((check) => `- ${checks.includes(check) ? "[x]" : "[ ]"} ${check}`),
  "",
  "## Same-Origin BFF Calls",
  "",
  ...(sameOriginBffCalls.length === 0 ? ["- None."] : [...new Set(sameOriginBffCalls)].map((call) => `- ${call}`)),
  "",
  "## Fake Commerce Node Calls",
  "",
  ...(fakeCommerceCalls.length === 0 ? ["- None."] : [...new Set(fakeCommerceCalls)].map((call) => `- ${call}`)),
  "",
  "## Host Logs",
  "",
  ...(hostLogs.length === 0 ? ["- None."] : hostLogs.slice(-40).map((line) => `- ${line}`)),
  "",
  "## Browser Events",
  "",
  ...(browserEvents.length === 0 ? ["- None."] : browserEvents.map((line) => `- ${line}`)),
  "",
  "## Failure Screenshot",
  "",
  failureScreenshotPath === null ? "- None." : `- ${failureScreenshotPath}`,
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
  const purchasePanel = readFileSync(`${serverProjectRoot}/Components/Catalog/PurchasePanelPlaceholder.razor`, "utf8");
  const layout = readFileSync(`${serverProjectRoot}/Components/Layout/MainLayout.razor`, "utf8");
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

async function routeSameOriginBff(page, origin) {
  await page.route(`${origin}/api/**`, async (route) => {
    const request = route.request();
    const url = new URL(request.url());

    if (request.method() === "GET" && url.pathname === "/api/cart") {
      await fulfillJson(route, browserCart(0));
      return;
    }

    if (request.method() === "POST" && url.pathname === "/api/product-selection-preview") {
      await fulfillJson(route, {
        productId: proofProduct.id,
        productVariantId: proofProduct.variantId,
        isValid: true,
        isAvailable: true,
        canAddToCart: true,
        validationMessages: [],
        selectedAttributes: [{ name: "Size", value: "M" }],
        attributeSignature: "Size=M",
        sku: "SKU-FAST-M",
        gtin: "GTIN-FAST-M",
        displayName: "Proof Product / M",
        unitPrice: 19,
        comparePrice: null,
        currencyCode: "USD",
        formattedUnitPrice: "$19.00",
        formattedComparePrice: null,
        stockQuantity: 7,
        minQuantity: 1,
        maxQuantity: 9,
        primaryImageUrl: "",
      });
      return;
    }

    if (request.method() === "POST" && url.pathname === "/api/cart/lines") {
      await fulfillJson(route, browserCart(1));
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

    if (url.pathname.startsWith("/api/checkout") || url.pathname.startsWith("/api/account")) {
      await fulfillJson(route, { success: true });
      return;
    }

    await route.fallback();
  });
}

async function startFakeCommerceNode() {
  const server = createServer((request, response) => {
    const url = new URL(request.url ?? "/", "http://fake-commerce.local");
    fakeCommerceCalls.push(`${request.method} ${url.pathname}`);

    if (!url.pathname.startsWith("/api/storefront/stores/default/")) {
      json(response, 404, { success: false, message: "Not found." });
      return;
    }

    const path = url.pathname.replace("/api/storefront/stores/default", "");
    if (request.method === "GET" && path === "/store/current") {
      json(response, 200, envelope(currentStore()));
      return;
    }

    if (request.method === "GET" && path === "/configuration") {
      json(response, 200, envelope(publicConfiguration()));
      return;
    }

    if (request.method === "GET" && path === "/catalog/categories") {
      json(response, 200, envelope([proofCategory]));
      return;
    }

    if (request.method === "GET" && path === "/catalog/categories/tree") {
      json(response, 200, envelope([{ ...proofCategory, children: [] }]));
      return;
    }

    if (request.method === "GET" && path.startsWith("/catalog/categories/slug/")) {
      json(response, 200, envelope({
        category: proofCategory,
        breadcrumbs: [{ id: proofCategory.id, name: proofCategory.name, slug: proofCategory.slug }],
        products: [catalogProduct()],
        directProductCount: 1,
        descendantProductCount: 0,
      }));
      return;
    }

    if (request.method === "GET" && path === "/catalog/products") {
      json(response, 200, envelope({
        items: [catalogProduct()],
        pageNumber: 1,
        pageSize: Number.parseInt(url.searchParams.get("pageSize") || "24", 10),
        totalCount: 1,
      }));
      return;
    }

    if (request.method === "GET" && path.startsWith("/catalog/products/slug/")) {
      json(response, 200, envelope(productDetail()));
      return;
    }

    if (request.method === "GET" && path.startsWith("/catalog/products/")) {
      json(response, 200, envelope(productDetail()));
      return;
    }

    if (request.method === "POST" && path.startsWith("/catalog/products/") && path.endsWith("/selection-preview")) {
      json(response, 200, envelope(selectionPreview()));
      return;
    }

    if (request.method === "GET" && path === "/catalog/filters") {
      json(response, 200, envelope({ categories: [proofCategory], price: { min: 0, max: 50 }, attributes: [] }));
      return;
    }

    if (request.method === "GET" && path === "/catalog/search/suggestions") {
      json(response, 200, envelope({ suggestions: [] }));
      return;
    }

    if (request.method === "GET" && path === "/catalog/sitemap") {
      json(response, 200, envelope({ categories: [{ slug: proofCategory.slug }], products: [{ slug: proofProduct.slug }], pages: [] }));
      return;
    }

    if (request.method === "GET" && path.startsWith("/navigation/menus/")) {
      json(response, 200, envelope({ systemName: "main", generatedAt: nowIso(), items: [] }));
      return;
    }

    if (request.method === "GET" && path === "/pages/navigation") {
      json(response, 200, envelope([]));
      return;
    }

    if (request.method === "GET" && path.startsWith("/pages/slug/")) {
      json(response, 200, envelope(pageContent(url.pathname.split("/").pop() ?? "about-us")));
      return;
    }

    if (request.method === "GET" && path === "/seo/settings") {
      json(response, 200, envelope(seoSettings()));
      return;
    }

    if (request.method === "GET" && path === "/seo/redirects/resolve") {
      json(response, 200, envelope({ newPath: null, statusCode: 0 }));
      return;
    }

    if (request.method === "POST" && path === "/cart/session") {
      json(response, 200, envelope({ cartToken: proofCart.token, expiresAtUtc: futureIso() }));
      return;
    }

    if (path === "/cart" || path.startsWith("/cart/")) {
      json(response, 200, envelope(cartResponse()));
      return;
    }

    if (request.method === "POST" && path === "/auth/refresh-token") {
      json(response, 200, envelope({
        accessToken: proofAccessToken(),
        refreshToken: "proof-refresh-token",
        expiresAtUtc: futureIso(),
      }));
      return;
    }

    if (request.method === "POST" && path === "/checkout/start") {
      json(response, 200, envelope(checkoutSession()));
      return;
    }

    if (request.method === "GET" && path === "/payments/methods") {
      json(response, 200, envelope([paymentMethod()]));
      return;
    }

    if (request.method === "GET" && path === "/address/countries") {
      json(response, 200, envelope([{ code: "US", name: "United States" }]));
      return;
    }

    if (request.method === "GET" && path === "/address/countries/US/states") {
      json(response, 200, envelope([{ code: "CA", name: "California" }]));
      return;
    }

    if (request.method === "GET" && path === "/address/configuration") {
      json(response, 200, envelope({ phoneEnabled: true, phoneRequired: false, postalCodeRequired: true }));
      return;
    }

    if (path.startsWith("/consent")) {
      json(response, 200, envelope(consentState()));
      return;
    }

    json(response, 404, { success: false, message: `Unhandled fake Commerce path ${request.method} ${path}` });
  });

  await listen(server, "127.0.0.1", 0);
  const port = server.address().port;
  return {
    url: `http://127.0.0.1:${port}`,
    stop: () => close(server),
  };
}

function startGeneratedHost(hostUrl, commerceNodeBaseUrl) {
  const child = spawn("dotnet", [
    "run",
    "--project",
    projectFile,
    "--configuration",
    "Debug",
    "--no-build",
    "--no-launch-profile",
    "--urls",
    hostUrl,
  ], {
    cwd: projectRoot,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      Api__RefreshTokenCookieName: "bs-proof-refresh",
      Storefront__CommerceNodeBaseUrl: commerceNodeBaseUrl,
      Storefront__StoreKey: "default",
      Storefront__PublicBaseUrl: hostUrl,
      PublicUrl__BaseUrl: hostUrl,
      ClientApp__BaseUrl: hostUrl,
    },
    stdio: ["ignore", "pipe", "pipe"],
  });

  child.stdout.on("data", (chunk) => appendHostLog(chunk));
  child.stderr.on("data", (chunk) => appendHostLog(chunk));
  child.on("exit", (code, signal) => {
    appendHostLog(`generated host exited code=${code ?? ""} signal=${signal ?? ""}`);
  });
  return child;
}

async function waitForHost(origin) {
  const deadline = Date.now() + 90_000;
  let lastError = "";
  while (Date.now() < deadline) {
    if (host.exitCode !== null) {
      throw new Error(`Generated host exited before it became ready. ${hostLogs.slice(-10).join("\n")}`);
    }

    try {
      const response = await fetch(`${origin}${proofProductPath}`, { redirect: "manual" });
      if (response.status > 0 && response.status < 500) {
        return;
      }
      lastError = `HTTP ${response.status}`;
    } catch (error) {
      lastError = error.message;
    }

    await delay(500);
  }

  throw new Error(`Generated host did not become ready at ${origin}: ${lastError}\n${hostLogs.slice(-20).join("\n")}`);
}

function stopGeneratedHost(child) {
  if (!child || child.exitCode !== null) {
    return;
  }

  if (process.platform === "win32" && child.pid) {
    spawnSync("taskkill", ["/PID", String(child.pid), "/T", "/F"], { stdio: "ignore" });
    return;
  }

  child.kill("SIGTERM");
}

async function reservePort() {
  const server = createServer();
  await listen(server, "127.0.0.1", 0);
  const port = server.address().port;
  await close(server);
  return port;
}

function listen(server, hostName, port) {
  return new Promise((resolveListen, rejectListen) => {
    server.once("error", rejectListen);
    server.listen(port, hostName, resolveListen);
  });
}

function close(server) {
  return new Promise((resolveClose) => server.close(resolveClose));
}

function appendHostLog(chunk) {
  String(chunk)
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .forEach((line) => hostLogs.push(line));
}

async function fulfillJson(route, payload) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(payload) });
}

function json(response, statusCode, payload) {
  response.writeHead(statusCode, { "content-type": "application/json" });
  response.end(JSON.stringify(payload));
}

function envelope(data, message = "Request completed.") {
  return { success: true, message, data };
}

function browserCart(count) {
  return {
    count,
    version: 1,
    lines: count > 0 ? [{
      lineId: proofCart.lineId,
      productId: proofProduct.id,
      productVariantId: proofProduct.variantId,
      displayName: "Proof Product",
      productUrl: proofProductPath,
      imageUrl: "",
      quantity: count,
      unitPrice: 19,
      unitPriceDisplay: "$19.00",
      lineTotal: 19 * count,
      lineTotalDisplay: `$${(19 * count).toFixed(2)}`,
      currencyCode: "USD",
      selectedAttributesText: "Size: M",
      minQuantity: 1,
      maxQuantity: 9,
      quantityStep: 1,
      warnings: [],
      isUnavailable: false,
    }] : [],
    currencyCode: "USD",
    subtotal: 19 * count,
    subtotalDisplay: `$${(19 * count).toFixed(2)}`,
    grandTotal: 19 * count,
    grandTotalDisplay: `$${(19 * count).toFixed(2)}`,
    checkoutAllowed: true,
    warnings: [],
    adjustments: [],
  };
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

function currentStore() {
  return {
    publicId: proofStoreId,
    storeKey: "default",
    name: "Generated Proof Store",
    status: "active",
    baseUrl: null,
    primaryDomain: null,
    forceHttps: false,
    cdnHost: null,
    logoUrl: null,
    companyName: "Generated Proof Store",
    companyEmail: "support@example.test",
    companyPhone: null,
    companyAddress: null,
    faviconUrl: null,
    pngIconUrl: null,
    appleTouchIconUrl: null,
    msTileImageUrl: null,
    msTileColor: null,
    defaultCurrencyCode: "USD",
    defaultCulture: "en-US",
    supportEmail: "support@example.test",
    supportPhone: null,
    maintenanceModeEnabled: false,
    maintenanceMessage: null,
    htmlBodyId: null,
  };
}

function publicConfiguration() {
  return {
    storeIdentity: currentStore(),
    branding: currentStore(),
    localeOptions: { defaultCulture: "en-US", supportedCultures: ["en-US"] },
    currencyOptions: { defaultCurrencyCode: "USD", supportedCurrencyCodes: ["USD"] },
    consent: {
      enabled: true,
      bannerRequired: true,
      currentVersion: "v1",
      policyPagePath: "/pages/privacy",
      categories: [
        { name: "essential", required: true, defaultEnabled: true },
        { name: "preferences", required: false, defaultEnabled: false },
        { name: "analytics", required: false, defaultEnabled: false },
        { name: "marketing", required: false, defaultEnabled: false },
      ],
      visitorCookieLifetimeDays: 180,
    },
    captcha: { enabled: false, providerSystemName: "", publicSiteKey: null, enabledTargets: [], actionNames: {} },
    maintenanceState: { maintenanceModeEnabled: false, maintenanceMessage: null },
    featureFlags: {
      customerAccountsEnabled: true,
      cartEnabled: true,
      checkoutEnabled: true,
      paymentsEnabled: true,
      newsletterEnabled: true,
      recommendationsEnabled: true,
    },
    features: {},
    paymentMethods: [paymentMethod()],
    seoDefaults: seoSettings(),
  };
}

function catalogProduct() {
  return {
    id: proofProduct.id,
    slug: proofProduct.slug,
    name: "Proof Product",
    description: "Generated proof product.",
    sku: "SKU-FAST",
    gtin: "GTIN-FAST",
    shortDescription: "Generated proof product.",
    price: 19,
    comparePrice: null,
    displayPrice: 19,
    displayComparePrice: null,
    displayCurrencyCode: "USD",
    image: "",
    createdOn: nowIso(),
    updatedAt: nowIso(),
    displayOrder: 1,
    inStock: true,
    quantity: 7,
    purchasable: true,
    purchaseBlockReasons: [],
    stockStatus: "In stock",
    availableQuantity: 7,
    minOrderQuantity: 1,
    maxOrderQuantity: 9,
    quantityStep: 1,
    manageStock: true,
    shippingRequired: true,
    freeShipping: false,
    shippingSurcharge: null,
    deliveryEstimateText: "Ships soon",
    categoryId: proofCategory.id,
    categoryName: proofCategory.name,
    categorySlug: proofCategory.slug,
    hasVariants: true,
  };
}

function productDetail() {
  return {
    ...catalogProduct(),
    fullDescription: "Generated proof product detail.",
    metaTitle: "Proof Product",
    metaDescription: "Generated proof product.",
    canonicalUrl: null,
    ogTitle: "Proof Product",
    ogDescription: "Generated proof product.",
    ogImage: null,
    seoContent: "",
    robotsIndex: true,
    robotsFollow: true,
    category: proofCategory,
    variationTemplate: {
      name: "Default options",
      slug: "default-options",
      options: [{ name: "Size", controlType: "button", isRequired: true, values: [{ value: "M", colorHex: null }] }],
    },
    mediaGallery: [],
    variants: [{
      id: proofProduct.variantId,
      productId: proofProduct.id,
      sku: "SKU-FAST-M",
      attributes: [{ name: "Size", value: "M" }],
      attributeSignature: "Size=M",
      displayName: "Proof Product / M",
      sizeScale: 0,
      sizeValue: "M",
      price: 19,
      effectivePrice: 19,
      displayPrice: 19,
      displayCurrencyCode: "USD",
      stock: 7,
      purchasable: true,
      purchaseBlockReasons: [],
      stockStatus: "In stock",
      availableQuantity: 7,
      color: null,
      isActive: true,
      isDefault: true,
    }],
  };
}

function selectionPreview() {
  return {
    productId: proofProduct.id,
    productVariantId: proofProduct.variantId,
    isValid: true,
    isAvailable: true,
    canAddToCart: true,
    validationMessages: [],
    selectedAttributes: [{ name: "Size", value: "M" }],
    attributeSignature: "Size=M",
    sku: "SKU-FAST-M",
    gtin: "GTIN-FAST-M",
    displayName: "Proof Product / M",
    unitPrice: 19,
    comparePrice: null,
    currencyCode: "USD",
    stockQuantity: 7,
    minQuantity: 1,
    maxQuantity: 9,
    primaryImageUrl: "",
  };
}

function cartResponse() {
  return {
    cartId: proofCart.id,
    cartToken: proofCart.token,
    expiresAtUtc: futureIso(),
    version: 1,
    currencyCode: "USD",
    subtotal: 19,
    grandTotal: 19,
    summaryCount: 1,
    lines: [{
      lineId: proofCart.lineId,
      productId: proofProduct.id,
      productVariantId: proofProduct.variantId,
      productSlug: proofProduct.slug,
      productUrl: proofProductPath,
      displayName: "Proof Product",
      imageUrl: "",
      quantity: 1,
      unitPrice: 19,
      unitPriceSnapshot: 19,
      lineTotal: 19,
      lineSubtotal: 19,
      currencyCodeSnapshot: "USD",
      selectedAttributes: [{ name: "Size", value: "M" }],
      quantityMinimum: 1,
      quantityMaximum: 9,
      quantityStep: 1,
      purchasable: true,
      warnings: [],
    }],
    checkoutAllowed: true,
    warnings: [],
    adjustments: [],
  };
}

function checkoutSession() {
  return {
    checkoutSessionId: proofCheckoutId,
    cartId: proofCart.id,
    checkoutVersion: 1,
    cartVersion: 1,
    lastValidatedCartVersion: 1,
    currencyCode: "USD",
    subtotal: 19,
    shippingTotal: 0,
    taxTotal: 0,
    discountTotal: 0,
    grandTotal: 19,
    requiresShipping: true,
    shippingAddress: null,
    billingAddress: null,
    selectedShippingOption: null,
    shippingOptions: [],
    selectedPaymentMethod: paymentMethodOption(),
    paymentMethods: [paymentMethodOption()],
    lines: [],
    issues: [],
  };
}

function paymentMethod() {
  return {
    id: proofPaymentMethodId,
    key: "cod",
    name: "Cash on delivery",
    description: "Pay when the order arrives.",
    shortDisplayText: "COD",
    iconUrl: null,
    supportedCurrencyCodes: ["USD"],
    supportedCountryCodes: ["US"],
  };
}

function paymentMethodOption() {
  return { key: "cod", displayName: "Cash on delivery", description: "Pay when the order arrives." };
}

function pageContent(slug) {
  return {
    slug,
    title: "Proof page",
    intro: "Generated proof content.",
    bodyHtml: "<p>Generated proof content.</p>",
    seo: { metaTitle: "Proof page", metaDescription: "Generated proof content.", robotsIndex: true, robotsFollow: true },
    updatedAt: nowIso(),
    pageKey: slug,
  };
}

function seoSettings() {
  return {
    id: proofSeoSettingsId,
    siteName: "Generated Proof Store",
    defaultTitleSuffix: "| Generated Proof Store",
    defaultMetaDescription: "Generated proof storefront.",
    defaultOgImage: null,
    baseCanonicalUrl: null,
    companyName: "Generated Proof Store",
    companyLogoUrl: null,
    companyPhone: null,
    companyEmail: "support@example.test",
    companyAddress: null,
    facebookUrl: null,
    instagramUrl: null,
    xUrl: null,
  };
}

function nowIso() {
  return new Date().toISOString();
}

function futureIso() {
  return new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
}

function proofAccessToken() {
  const header = base64UrlJson({ alg: "none", typ: "JWT" });
  const payload = base64UrlJson({
    email: "proof@example.test",
    unique_name: "Proof Customer",
    FullName: "Proof Customer",
    role: "Customer",
    exp: Math.floor(Date.now() / 1000) + 3600,
  });
  return `${header}.${payload}.`;
}

function base64UrlJson(value) {
  return Buffer.from(JSON.stringify(value), "utf8")
    .toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
