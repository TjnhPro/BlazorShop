const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const repoRoot = path.resolve(__dirname, "../..");
const visualScript = path.join(repoRoot, "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
const baseUrl = "http://storefront-browser-semantics-proof.test";

function pageHtml() {
  return `<!DOCTYPE html>
<html>
<body>
  <main>
    <section data-storefront-product-gallery>
      <img data-storefront-gallery-main-image src="/media/products/initial.webp" alt="Initial" />
      <div data-storefront-gallery-placeholder hidden></div>
    </section>
    <section data-storefront-product-purchase>
      <span data-storefront-selection-price>USD 10.00</span>
      <span data-storefront-selection-compare>USD 12.00</span>
      <span data-storefront-selection-stock>3 in stock</span>
      <span data-storefront-selection-sku>SKU OLD</span>
      <span data-storefront-selection-gtin>GTIN OLD</span>
      <button type="button" data-storefront-product-purchase-submit>Add to Cart</button>
      <p data-storefront-purchase-feedback></p>
    </section>
  </main>
</body>
</html>`;
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    await page.route(`${baseUrl}/**`, async (route) => {
      await route.fulfill({ status: 200, contentType: "text/html", body: pageHtml() });
    });

    await page.goto(baseUrl, { waitUntil: "domcontentloaded" });
    await page.addScriptTag({ path: visualScript });
    await dispatchSelection(page, {
      valid: true,
      ready: true,
      priceText: "USD 19.00",
      comparePriceText: "",
      stockText: "7 in stock",
      skuText: "SKU BLUE-L",
      gtinText: "GTIN 000111222333",
      mainImageUrl: "/media/products/blue-large.webp",
      message: "Selection ready.",
    });

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

    assert(state.price === "USD 19.00", `price did not update: ${JSON.stringify(state)}`);
    assert(state.compare === "", `compare price did not clear: ${JSON.stringify(state)}`);
    assert(state.compareHidden, `compare price did not hide: ${JSON.stringify(state)}`);
    assert(state.stock === "7 in stock", `stock did not update: ${JSON.stringify(state)}`);
    assert(state.sku === "SKU BLUE-L" && !state.skuHidden, `SKU did not update: ${JSON.stringify(state)}`);
    assert(state.gtin === "GTIN 000111222333" && !state.gtinHidden, `GTIN did not update: ${JSON.stringify(state)}`);
    assert(state.imageSrc.endsWith("/media/products/blue-large.webp"), `image did not update: ${JSON.stringify(state)}`);
    assert(!state.submitDisabled, `submitter should stay enabled: ${JSON.stringify(state)}`);

    await dispatchSelection(page, {
      valid: true,
      ready: false,
      priceText: "USD 19.00",
      comparePriceText: "",
      stockText: "Out of stock",
      skuText: "",
      gtinText: "",
      mainImageUrl: "/media/products/blue-large.webp",
      message: "This selection is not available.",
    });

    const emptyState = await page.evaluate(() => ({
      skuHidden: document.querySelector("[data-storefront-selection-sku]")?.classList.contains("hidden") || false,
      gtinHidden: document.querySelector("[data-storefront-selection-gtin]")?.classList.contains("hidden") || false,
      submitDisabled: document.querySelector("[data-storefront-product-purchase-submit]")?.disabled || false,
    }));

    assert(emptyState.skuHidden, `empty SKU did not hide: ${JSON.stringify(emptyState)}`);
    assert(emptyState.gtinHidden, `empty GTIN did not hide: ${JSON.stringify(emptyState)}`);
    assert(emptyState.submitDisabled, `submitter should be disabled when not ready: ${JSON.stringify(emptyState)}`);

    console.log(JSON.stringify({ ok: true, state, emptyState }, null, 2));
  } finally {
    await browser.close();
  }
}

async function dispatchSelection(page, selection) {
  await page.evaluate((detail) => {
    const root = document.querySelector("[data-storefront-product-purchase]");
    root.dispatchEvent(new CustomEvent("storefront:product-purchase:selection-changed", {
      bubbles: true,
      detail: {
        root,
        selection: detail,
      },
    }));
  }, selection);
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
