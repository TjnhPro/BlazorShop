const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const baseUrl = trimEnd(process.env.STOREFRONT_BASE_URL || "http://localhost:18598", "/");
const productSlug = process.env.STOREFRONT_QA_VARIANT_PRODUCT_SLUG || "catalog-qa-t-shirt";
const directCommerceCalls = [];
const sameOriginCalls = [];
const previewCalls = [];

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
    });

    await page.route(`${baseUrl}/api/product-selection-preview`, async (route) => {
      const request = route.request();
      const body = readJsonPostData(request);
      previewCalls.push({ method: request.method(), body });

      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isValid: true,
          isAvailable: true,
          canAddToCart: true,
          productId: body?.ProductId || body?.productId || "",
          productVariantId: body?.ProductVariantId || body?.productVariantId || "",
          selectedAttributes: body?.SelectedAttributes || body?.selectedAttributes || [],
          quantity: body?.Quantity || body?.quantity || 1,
          currencyCode: body?.CurrencyCode || body?.currencyCode || "USD",
          unitPrice: 21.99,
          formattedUnitPrice: "$21.99",
          formattedComparePrice: "",
          stockQuantity: 3,
          sku: "QA-TSHIRT-RED-XL",
          gtin: "000111222333",
          primaryImageUrl: "/images/banner-bg.jpg?variant=red-xl",
          message: "Selection ready."
        })
      });
    });

    const response = await page.goto(`${baseUrl}/product/${productSlug}`, { waitUntil: "domcontentloaded", timeout: 60000 });
    assert(response?.ok(), `product page returned ${response?.status() ?? "no response"}`);

    await page.waitForSelector("[data-storefront-product-purchase]", { timeout: 15000 });
    await page.waitForSelector("[data-storefront-product-purchase-submit]", { timeout: 15000 });
    await selectRedXlOption(page);

    await page.waitForFunction(() => {
      const text = (selector) => document.querySelector(selector)?.textContent?.trim() || "";
      return text("[data-storefront-selection-price]").includes("21.99")
        && text("[data-storefront-selection-stock]").includes("3")
        && text("[data-storefront-selection-sku]").includes("QA-TSHIRT-RED-XL")
        && text("[data-storefront-selection-gtin]").includes("000111222333");
    }, null, { timeout: 15000 });

    const state = await page.evaluate(() => {
      const text = (selector) => document.querySelector(selector)?.textContent?.trim() || "";
      const hidden = (selector) => document.querySelector(selector)?.classList.contains("hidden") || false;
      const image = document.querySelector("[data-storefront-gallery-main-image]");
      const submit = document.querySelector("[data-storefront-product-purchase-submit]");

      return {
        price: text("[data-storefront-selection-price]"),
        compare: text("[data-storefront-selection-compare]"),
        compareHidden: hidden("[data-storefront-selection-compare]"),
        stock: text("[data-storefront-selection-stock]"),
        sku: text("[data-storefront-selection-sku]"),
        skuHidden: hidden("[data-storefront-selection-sku]"),
        gtin: text("[data-storefront-selection-gtin]"),
        gtinHidden: hidden("[data-storefront-selection-gtin]"),
        imageSrc: image?.getAttribute("src") || "",
        submitDisabled: submit?.disabled || false,
      };
    });

    assert(state.price.includes("21.99"), `price did not update: ${JSON.stringify(state)}`);
    assert(state.stock.includes("3"), `stock did not update: ${JSON.stringify(state)}`);
    assert(state.sku.includes("QA-TSHIRT-RED-XL") && !state.skuHidden, `SKU did not update: ${JSON.stringify(state)}`);
    assert(state.gtin.includes("000111222333") && !state.gtinHidden, `GTIN did not update: ${JSON.stringify(state)}`);
    assert(state.imageSrc.includes("variant=red-xl"), `image did not update: ${JSON.stringify(state)}`);
    assert(!state.submitDisabled, `submitter should stay enabled: ${JSON.stringify(state)}`);
    assert(directCommerceCalls.length === 0, `direct Commerce calls detected: ${directCommerceCalls.join(", ")}`);
    assert(sameOriginCalls.includes("POST /api/product-selection-preview"), "missing same-origin product preview call");
    assert(previewCalls.length > 0, "missing recorded product preview request");

    console.log(JSON.stringify({
      ok: true,
      baseUrl,
      productSlug,
      state,
      sameOriginCalls: [...new Set(sameOriginCalls)],
      previewCalls: previewCalls.length,
    }, null, 2));
  } finally {
    await browser.close();
  }
}

async function selectRedXlOption(page) {
  const variantSelect = page.locator("[data-storefront-purchase-variant]").first();
  if (await variantSelect.count() > 0) {
    const value = await variantSelect.evaluate((select) => {
      const option = [...select.options].find((candidate) =>
        /red/i.test(candidate.textContent || candidate.dataset.displayName || "")
        && /xl/i.test(candidate.textContent || candidate.dataset.displayName || ""));
      return option?.value || "";
    });
    assert(value, "Red / XL variant option was not found.");
    await variantSelect.selectOption(value);
    await variantSelect.dispatchEvent("change");
    return;
  }

  await page.locator("[data-storefront-purchase-attribute][data-storefront-purchase-attribute-name='Color'][value='Red']").first().check();
  await page.locator("[data-storefront-purchase-attribute][data-storefront-purchase-attribute-name='Size'][value='XL']").first().check();
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
