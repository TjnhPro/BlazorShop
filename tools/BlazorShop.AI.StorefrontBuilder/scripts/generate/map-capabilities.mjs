#!/usr/bin/env node
import { readFileSync, mkdirSync, writeFileSync, existsSync } from "node:fs";
import { dirname } from "node:path";

const output = readArg("--output") ?? "obj/storefront-builder/capability-decisions.yaml";
const manifestPath = readArg("--feature-manifest") ?? "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Features/feature-manifest.json";
const contractPath = readArg("--contract") ?? "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml";

const featureManifest = existsSync(manifestPath)
  ? JSON.parse(readFileSync(manifestPath, "utf8"))
  : { features: {} };
const contract = existsSync(contractPath) ? readFileSync(contractPath, "utf8") : "";
const slots = new Set([...contract.matchAll(/^\s+- id:\s+([a-z0-9.-]+)\s*$/gim)].map((match) => match[1]));

const detections = [
  detected("product-gallery", "Product gallery visual exists and Starter slot exists.", "product.gallery", "gallery"),
  detected("wishlist", "Wishlist visual exists but backend capability missing.", "", "wishlist"),
  detected("checkout", "Target checkout unavailable for MVP.", "checkout.page", "checkout"),
  detected("product-reviews", "Product reviews visual exists but module unavailable.", "", "reviews"),
  detected("cart-badge", "Cart badge visual exists and BFF slot exists.", "layout.cart-badge", "cart"),
  detected("recommendations", "Recommendations visual exists and Starter feature exists.", "home.sections", "recommendations"),
];

const decisions = detections.map((item) => {
  const feature = featureManifest.features?.[item.featureKey];
  const featureInstalled = Boolean(feature?.installed);
  const slotExists = item.slotId.length > 0 && slots.has(item.slotId);
  let decision = "unsupported";
  let fallbackDecision = "Show Starter fallback or hide target-only affordance.";

  if (item.featureKey === "checkout") {
    decision = "starter";
    fallbackDecision = "Use Starter checkout route and theme fallback page.";
  } else if (slotExists && item.featureKey !== "wishlist" && item.featureKey !== "reviews") {
    decision = "target-with-starter-binding";
    fallbackDecision = "Bind target presentation through Starter slot/action contract.";
  } else if (featureInstalled && slotExists) {
    decision = "target-with-starter-binding";
    fallbackDecision = "Bind target presentation through Starter slot/action contract.";
  } else if (featureInstalled) {
    decision = "target";
    fallbackDecision = "Render presentation only; no commerce state mutation.";
  } else if (item.featureKey === "wishlist" || item.featureKey === "reviews") {
    decision = "hidden";
    fallbackDecision = "Hide unsupported target module affordance.";
  }

  return {
    featureId: item.featureId,
    featureKey: item.featureKey,
    targetDetection: item.targetDetection,
    slotId: item.slotId || "none",
    decision,
    fallbackDecision,
    evidenceIds: [`evidence-${item.featureId}`],
  };
});

const artifact = {
  schemaVersion: "1.0.0",
  artifactKind: "capability-decisions",
  artifactId: "capability-decisions.default",
  inputs: [
    "Starter feature manifest",
    "Backend public configuration feature map",
    "Store module manifest if available",
    "Target visual detections",
    "Starter generation contract slots",
  ],
  decisionValues: ["target", "target-with-starter-binding", "starter", "hidden", "unsupported"],
  decisions,
};

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${toYaml(artifact)}\n`, "utf8");

function detected(featureId, targetDetection, slotId, featureKey) {
  return { featureId, targetDetection, slotId, featureKey };
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
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
