#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";

const args = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  args.set(process.argv[index], process.argv[index + 1]);
}

const behaviorsPath = args.get("--behaviors-output") ?? "obj/storefront-builder/behaviors.yaml";
const responsivePath = args.get("--responsive-output") ?? "obj/storefront-builder/responsive.yaml";

const behaviors = {
  artifactType: "storefront-builder.behaviors",
  version: 1,
  rules: [
    "Commerce state changes must be Starter-feature-driven or BFF-action-driven.",
    "JS interop cannot own cart/checkout/account state.",
    "Unsupported target behavior must be hidden or replaced by Starter fallback.",
  ],
  behaviors: [
    behavior("css-only", "hover-card-shadow", "product-card", "visual", "starter"),
    behavior("hover-driven", "navigation-hover", "main-navigation", "visual", "target-with-starter-binding"),
    behavior("focus-driven", "search-focus-ring", "search-input", "visual", "target-with-starter-binding"),
    behavior("click-driven visual-only", "mobile-menu-toggle", "mobile-navigation", "visual", "target-with-starter-binding"),
    behavior("scroll-driven visual-only", "sticky-header-shadow", "header", "visual", "target"),
    behavior("Starter-feature-driven", "cart-badge-refresh", "cart-badge", "commerce-state", "target-with-starter-binding"),
    behavior("BFF-action-driven", "add-to-cart", "product-purchase-block", "commerce-state", "target-with-starter-binding"),
    behavior("Approved JS interop", "drawer-focus-trap", "mobile-navigation", "accessibility", "target-with-starter-binding"),
    behavior("Unsupported", "wishlist-toggle", "product-card", "commerce-state", "hidden", "Backend capability missing; hide target visual affordance."),
  ],
};

const responsive = {
  artifactType: "storefront-builder.responsive",
  version: 1,
  breakpoints: [
    responsiveRecord("mobile", "0-639px", "single-column", "drawer navigation", 1, "media above purchase action", "stacked", "header fixed only while drawer open", "slide-over drawer"),
    responsiveRecord("tablet", "640-1023px", "two-column sections", "collapsed primary navigation", 2, "media above purchase action", "two-column footer", "sticky purchase disabled", "drawer with wider panel"),
    responsiveRecord("desktop", "1024px+", "content grid", "horizontal navigation", 4, "gallery beside purchase action", "multi-column footer", "sticky header shadow", "hover menu"),
  ],
};

writeYaml(behaviorsPath, behaviors);
writeYaml(responsivePath, responsive);

function behavior(behaviorClass, behaviorId, patternId, stateOwnership, decision, fallbackBehavior = "starter slot/action contract") {
  return {
    behaviorId,
    class: behaviorClass,
    patternId,
    stateOwnership,
    decision,
    interactionOwner: behaviorId === "add-to-cart" ? "BFF-action-driven" : behaviorClass,
    evidenceIds: [`evidence-${behaviorId}`],
    fallbackBehavior,
  };
}

function responsiveRecord(breakpoint, range, layoutChange, headerNavBehavior, productGridColumns, productDetailMediaActionStacking, footerStacking, stickyFixedElements, drawerMenuBehavior) {
  return {
    breakpoint,
    range,
    layoutChange,
    headerNavBehavior,
    productGridColumns,
    productDetailMediaActionStacking,
    footerStacking,
    stickyFixedElements,
    drawerMenuBehavior,
  };
}

function writeYaml(path, value) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${toYaml(value)}\n`, "utf8");
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
  if (typeof value === "number") {
    return String(value);
  }

  const text = String(value);
  return /^[a-zA-Z0-9_.\/+-]+$/.test(text) ? text : JSON.stringify(text);
}
