#!/usr/bin/env node
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";

const output = readArg("--output") ?? "obj/storefront-builder/composition-manifest.yaml";
const projectName = readArg("--project-name") ?? "BlazorShop.Storefront.Demo";
const storeKey = readArg("--store-key") ?? "demo";
const sourceStarterPath = readArg("--starter-path") ?? "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter";
const contractPath = `${sourceStarterPath}/starter-generation.contract.yaml`;
const packageVersionsPath = `${sourceStarterPath}/StorefrontPackageVersions.props`;

const contract = readFileSync(contractPath, "utf8");
const packageVersions = readFileSync(packageVersionsPath, "utf8");

const manifest = {
  schemaVersion: "1.0.0",
  artifactKind: "composition-manifest",
  artifactId: "composition-manifest.default",
  projectName,
  storeKey,
  sourceStarterPath,
  starterContractVersion: matchValue(contract, /contractVersion:\s*([^\r\n]+)/, "unknown"),
  packageVersions: {
    StorefrontClientPackageVersion: matchValue(packageVersions, /<StorefrontClientPackageVersion>([^<]+)<\/StorefrontClientPackageVersion>/, "unknown"),
    StorefrontRuntimePackageVersion: matchValue(packageVersions, /<StorefrontRuntimePackageVersion>([^<]+)<\/StorefrontRuntimePackageVersion>/, "unknown"),
  },
  generatedFileRoot: `BlazorShop.PresentationV2/${projectName}`,
  assetRoot: `BlazorShop.PresentationV2/${projectName}/wwwroot/assets/generated`,
  shellComposition: ["layout.header", "layout.main-navigation", "layout.mobile-navigation", "layout.cart-badge", "layout.account-menu", "layout.footer"],
  pageComposition: [
    page("/", "home.sections", "InitialSnapshot"),
    page("/category/{Slug}", "catalog.product-card", "BrowserFetch"),
    page("/search", "catalog.product-card", "BrowserFetch"),
    page("/product/{Slug}", "product.purchase", "BrowserFetch"),
    page("/cart", "cart.page", "BrowserFetch"),
    page("/checkout", "checkout.page", "BrowserFetch"),
    page("/account", "account.shell", "BrowserFetch"),
  ],
  slotBindings: [
    binding("layout.header", "Components/Layout/MainLayout.razor", "target-with-starter-binding"),
    binding("layout.footer", "Components/Layout/MainLayout.razor", "target-with-starter-binding"),
    binding("catalog.product-card", "Components/Catalog/ProductSummaryCard.razor", "target-with-starter-binding"),
    binding("product.gallery", "Components/Catalog/ProductGalleryPlaceholder.razor", "target-with-starter-binding"),
    binding("product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor", "target-with-starter-binding", "cart.add-line"),
    binding("cart.page", "Pages/Hybrid/Commerce/CartPage.razor", "starter"),
    binding("checkout.page", "Pages/Hybrid/Commerce/CheckoutPage.razor", "starter"),
    binding("account.shell", "Pages/WasmHost/Account/AccountHostPage.razor", "starter"),
  ],
  featureDecisions: ["product-gallery", "cart-badge", "wishlist:hidden", "product-reviews:hidden", "checkout:starter"],
  fallbackPages: ["cart.page", "checkout.page", "account.shell", "system.error"],
  evidenceReferences: ["page-inventory.yaml", "design-tokens.yaml", "ui-patterns.yaml", "page-topology.yaml", "capability-decisions.yaml"],
  inferenceReferences: ["ai-inference-log.json"],
};

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${toYaml(manifest)}\n`, "utf8");

function page(route, primarySlot, hydrationMode) {
  return { route, primarySlot, hydrationMode };
}

function binding(slotId, path, decision, action = "none") {
  return { slotId, path, decision, action };
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function matchValue(text, regex, fallback) {
  const match = regex.exec(text);
  return match ? match[1].trim() : fallback;
}

function toYaml(value, indent = 0) {
  const pad = " ".repeat(indent);
  if (Array.isArray(value)) {
    return value.map((item) => `${pad}- ${renderYamlItem(item, indent)}`).join("\n");
  }

  if (value && typeof value === "object") {
    return Object.entries(value)
      .map(([key, item]) => {
        if (Array.isArray(item) || (item && typeof item === "object")) {
          return `${pad}${key}:\n${toYaml(item, indent + 2)}`;
        }

        return `${pad}${key}: ${quoteScalar(item)}`;
      })
      .join("\n");
  }

  return `${pad}${quoteScalar(value)}`;
}

function renderYamlItem(item, indent) {
  if (item && typeof item === "object" && !Array.isArray(item)) {
    const entries = Object.entries(item);
    const [firstKey, firstValue] = entries[0];
    const rest = Object.fromEntries(entries.slice(1));
    const first = `${firstKey}: ${quoteScalar(firstValue)}`;
    const tail = Object.keys(rest).length === 0 ? "" : `\n${toYaml(rest, indent + 2)}`;
    return `${first}${tail}`;
  }

  return quoteScalar(item);
}

function quoteScalar(value) {
  if (Array.isArray(value)) {
    return `[${value.map((item) => quoteScalar(item)).join(", ")}]`;
  }

  const text = String(value);
  return text.length === 0 ? "\"\"" : /^[a-zA-Z0-9_.\/{}+-]+$/.test(text) ? text : JSON.stringify(text);
}
