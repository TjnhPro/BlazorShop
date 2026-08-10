const fs = require("fs");
const path = require("path");
const { chromium } = require(path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright"));

const baseUrl = trimEnd(process.env.STOREFRONT_BASE_URL || "http://127.0.0.1:18640", "/");
const phase = (process.env.STOREFRONT_COMPONENT_MVP_PHASE || "raw-html").toLowerCase();
const artifactRoot = path.resolve(__dirname, "../../output/playwright/storefront-component-mvp");
const steps = [];

async function main() {
  fs.mkdirSync(artifactRoot, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });

  try {
    if (phase !== "raw-html") {
      throw new Error(`Unsupported Component MVP proof phase '${phase}'.`);
    }

    await assertRawHtml(context);

    const evidence = {
      ok: true,
      phase,
      baseUrl,
      generatedAtUtc: new Date().toISOString(),
      steps,
    };

    fs.writeFileSync(path.join(artifactRoot, `${phase}.evidence.json`), JSON.stringify(evidence, null, 2));
    console.log(JSON.stringify(evidence, null, 2));
  } finally {
    await context.close();
    await browser.close();
  }
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
  };
  fs.mkdirSync(artifactRoot, { recursive: true });
  fs.writeFileSync(path.join(artifactRoot, `${phase}.evidence.failed.json`), JSON.stringify(failure, null, 2));
  console.error(error);
  process.exit(1);
});
