const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const baseUrl = trimEnd(process.env.STOREFRONT_BASE_URL || "http://localhost:18598", "/");
const productSlug = process.env.STOREFRONT_QA_PRODUCT_SLUG || "qa-simple-product-100";
const directCommerceCalls = [];
const sameOriginCalls = [];
const previewCalls = [];
const addLineCalls = [];

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    page.on("request", (request) => {
      const url = new URL(request.url());

      if (url.href.includes("/api/storefront/") || url.href.includes("/api/commerce/") || url.origin === "http://localhost:5180") {
        directCommerceCalls.push(`${request.method()} ${url.href}`);
      }

      if (url.origin === baseUrl && url.pathname.startsWith("/api/")) {
        sameOriginCalls.push(`${request.method()} ${url.pathname}`);
      }

      if (url.origin === baseUrl && url.pathname === "/api/product-selection-preview") {
        previewCalls.push({ method: request.method(), body: readJsonPostData(request) });
      }

      if (url.origin === baseUrl && url.pathname === "/api/cart/lines") {
        addLineCalls.push({ method: request.method(), body: readJsonPostData(request) });
      }
    });

    const productResponse = await page.goto(`${baseUrl}/product/${productSlug}`, { waitUntil: "domcontentloaded", timeout: 60000 });
    assert(productResponse?.ok(), `product page returned ${productResponse?.status() ?? "no response"}`);

    await page.waitForSelector("[data-storefront-product-purchase]", { timeout: 15000 });
    await page.waitForSelector("[data-storefront-purchase-quantity]", { timeout: 15000 });
    await page.waitForSelector("[data-storefront-product-purchase-submit]", { timeout: 15000 });

    const descriptors = await page.evaluate(() => {
      const root = document.querySelector("[data-storefront-product-purchase]");
      const quantity = document.querySelector("[data-storefront-purchase-quantity]");
      const submit = document.querySelector("[data-storefront-product-purchase-submit]");

      return {
        productId: root?.getAttribute("data-product-id") || "",
        currencyCode: root?.getAttribute("data-currency-code") || "",
        previewRoute: root?.getAttribute("data-selection-preview-route") || "",
        command: submit?.getAttribute("data-storefront-command") || "",
        quantity: quantity?.value || "",
      };
    });

    assert(descriptors.productId, `missing product descriptor: ${JSON.stringify(descriptors)}`);
    assert(descriptors.previewRoute === "/api/product-selection-preview", `unexpected preview route: ${JSON.stringify(descriptors)}`);
    assert(descriptors.command === "cart.add-line", `unexpected command descriptor: ${JSON.stringify(descriptors)}`);

    const quantity = page.locator("[data-storefront-purchase-quantity]").first();
    const previewAfterQuantity = page.waitForResponse((response) => {
      const url = new URL(response.url());
      return url.origin === baseUrl && url.pathname === "/api/product-selection-preview" && response.request().method() === "POST";
    }, { timeout: 20000 });
    await quantity.fill("2");
    await quantity.dispatchEvent("change");
    const previewResponse = await previewAfterQuantity;
    assert(previewResponse.ok(), `/api/product-selection-preview returned ${previewResponse.status()}`);

    const addResponsePromise = page.waitForResponse((response) => {
      const url = new URL(response.url());
      return url.origin === baseUrl && url.pathname === "/api/cart/lines" && response.request().method() === "POST";
    }, { timeout: 20000 });
    await page.locator("[data-storefront-product-purchase-submit]").first().click();
    const addResponse = await addResponsePromise;
    assert(addResponse.ok(), `/api/cart/lines returned ${addResponse.status()}`);

    await page.waitForFunction(() => {
      const text = document.querySelector("[data-storefront-cart-badge]")?.textContent?.trim() || "";
      const count = Number.parseInt(text, 10);
      return Number.isFinite(count) && count >= 1;
    }, null, { timeout: 15000 });

    const cartResponse = await page.goto(`${baseUrl}/my-cart`, { waitUntil: "domcontentloaded", timeout: 60000 });
    assert(cartResponse?.ok(), `cart page returned ${cartResponse?.status() ?? "no response"}`);
    const cartBody = await page.locator("body").innerText({ timeout: 10000 });
    assert(/qa|simple|product|cart/i.test(cartBody), "cart page did not render recognizable cart/product text");

    assert(directCommerceCalls.length === 0, `direct Commerce calls detected: ${directCommerceCalls.join(", ")}`);
    assert(sameOriginCalls.includes("POST /api/product-selection-preview"), "missing same-origin product preview call");
    assert(sameOriginCalls.includes("POST /api/cart/lines"), "missing same-origin add-line call");
    assert(previewCalls.length > 0, "missing recorded product preview request");
    assert(addLineCalls.length === 1, `expected one add-line request, got ${addLineCalls.length}`);

    console.log(JSON.stringify({
      ok: true,
      baseUrl,
      productSlug,
      descriptors,
      sameOriginCalls: [...new Set(sameOriginCalls)],
      previewCalls: previewCalls.length,
      addLineCalls: addLineCalls.length,
    }, null, 2));
  } finally {
    await browser.close();
  }
}

function readJsonPostData(request) {
  const body = request.postData();
  return body ? JSON.parse(body) : null;
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
