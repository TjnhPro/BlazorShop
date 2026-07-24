#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";

const output = readArg("--output") ?? "obj/storefront-builder/page-topology.yaml";

const regions = [
  region("global-shell", "", "layout.header", "Layout", "InitialSnapshot", "target-with-starter-binding", ["header"], "desktop horizontal nav; mobile drawer"),
  region("global-shell", "", "layout.footer", "Layout", "InitialSnapshot", "target-with-starter-binding", ["footer"], "footer stacks on mobile"),
  region("global-shell", "", "layout.main-navigation", "Layout", "InitialSnapshot", "target-with-starter-binding", ["main-navigation"], "horizontal desktop navigation"),
  region("global-shell", "", "layout.mobile-navigation", "Layout", "BrowserFetch", "target-with-starter-binding", ["mobile-navigation"], "drawer menu behavior"),
  region("global-shell", "", "layout.cart-badge", "Layout", "BrowserFetch", "target-with-starter-binding", ["cart-badge"], "BFF cart summary binding"),
  region("global-shell", "", "layout.account-menu", "Layout", "BrowserFetch", "starter", ["account-menu"], "Starter account fallback"),
  region("home-page-sections", "global-shell", "home.sections", "SSR", "InitialSnapshot", "target-with-starter-binding", ["banner-hero-section", "product-grid"], "target section order"),
  region("catalog-page-regions", "global-shell", "catalog.product-card", "Hybrid", "InitialSnapshot", "target-with-starter-binding", ["product-card"], "responsive product grid columns"),
  region("catalog-page-regions", "global-shell", "catalog.filters", "Hybrid", "BrowserFetch", "starter", ["filter-presentation"], "drawer on mobile"),
  region("catalog-page-regions", "global-shell", "catalog.sorting", "Hybrid", "BrowserFetch", "target-with-starter-binding", ["select"], "select presentation"),
  region("catalog-page-regions", "global-shell", "catalog.pagination", "Hybrid", "BrowserFetch", "target-with-starter-binding", ["pagination"], "pagination wraps on mobile"),
  region("search-result-page-regions", "global-shell", "search.page", "Hybrid", "BrowserFetch", "starter", ["search-input", "product-grid"], "uses catalog fallback style"),
  region("product-detail-regions", "global-shell", "product.gallery", "Hybrid", "InitialSnapshot", "target-with-starter-binding", ["product-gallery"], "gallery above purchase on mobile"),
  region("product-detail-regions", "global-shell", "product.information", "Hybrid", "InitialSnapshot", "target-with-starter-binding", ["product-information-block"], "information below gallery on mobile"),
  region("product-detail-regions", "global-shell", "product.purchase", "Hybrid", "BrowserFetch", "target-with-starter-binding", ["product-purchase-block", "quantity-control"], "BFF add-line action binding"),
  region("cart-fallback-style-regions", "global-shell", "cart.page", "Hybrid", "BrowserFetch", "starter", ["cart-page"], "themed Starter fallback"),
  region("checkout-fallback-style-regions", "global-shell", "checkout.page", "Hybrid", "BrowserFetch", "starter", ["checkout-page"], "themed Starter fallback"),
  region("account-fallback-style-regions", "global-shell", "account.shell", "WASM-host", "BrowserFetch", "starter", ["account-shell"], "themed Starter fallback"),
  region("content-error-system-page-shell", "global-shell", "system.error", "SSR", "InitialSnapshot", "starter", ["error-state"], "themed system state"),
];

const topology = {
  schemaVersion: "1.0.0",
  artifactKind: "page-topology",
  artifactId: "page-topology.default",
  regions,
  skippedSlots: [
    {
      slotId: "search.page",
      reason: "Search page is a route topology helper, not a Starter contract slot.",
    },
  ],
};

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${toYaml(topology)}\n`, "utf8");

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function region(regionId, parentRegion, slotId, renderOwner, hydrationMode, source, evidenceIds, responsiveBehavior) {
  return {
    regionId,
    parentRegion,
    slotId,
    renderOwner,
    hydrationMode,
    source,
    evidenceIds,
    responsiveBehavior,
  };
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
  return text.length === 0 ? "\"\"" : /^[a-zA-Z0-9_.\/+-]+$/.test(text) ? text : JSON.stringify(text);
}
