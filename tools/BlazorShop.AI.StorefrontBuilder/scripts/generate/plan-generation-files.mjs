#!/usr/bin/env node
import { createHash } from "node:crypto";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { buildHandoffGenerationPlan, stableJson, summarizePlan } from "./handoff-generation-plan.mjs";

const output = readArg("--output") ?? "obj/storefront-builder/generation-plan.yaml";
const jsonOutput = readArg("--json-output") ?? output.replace(/\.(yaml|yml)$/i, ".json");
const dryRun = process.argv.includes("--dry-run");
const projectName = readArg("--project-name") ?? "BlazorShop.Storefront.GeneratedProof";
const storeKey = readArg("--store-key") ?? "sample";
const outputRoot = readArg("--output-root") ?? "artifacts/storefront-builder/generated";
const handoffRoot = readArg("--handoff-root");
const repoRoot = readArg("--repo-root") ?? process.cwd();
const root = `${outputRoot.replaceAll("\\", "/").replace(/\/$/, "")}/${projectName}`;
const specHash = sha("composition-manifest.default");

if (handoffRoot) {
  const plan = buildHandoffGenerationPlan({
    repoRoot,
    handoffRoot,
    projectName,
    storeKey,
    outputRoot,
  });

  if (dryRun) {
    for (const line of summarizePlan(plan)) {
      console.log(line);
    }

    for (const file of plan.files) {
      console.log(`${file.action.padEnd(7)} ${file.ownership.padEnd(9)} ${file.targetPath}`);
    }
  }

  mkdirSync(dirname(output), { recursive: true });
  mkdirSync(dirname(jsonOutput), { recursive: true });
  writeFileSync(output, `${toYaml(plan)}\n`, "utf8");
  writeFileSync(jsonOutput, stableJson(plan), "utf8");
  process.exit(0);
}

const files = [
  entry(`${root}/wwwroot/css/storefront-builder.generated.css`, "generated", "replace", ["design-tokens.yaml", "ui-patterns.yaml"], "theme.foundation", ["SFB-CSS-001"], "replace only when generated hash matches"),
  entry(`${root}/Components/Layout/MainLayout.razor`, "generated", "patch", ["composition-manifest.yaml", "ui-patterns.yaml"], "layout.header", ["SFB-COMP-001"], "patch generated zones only"),
  entry(`${root}/Components/Catalog/ProductSummaryCard.razor`, "generated", "replace", ["ui-patterns.yaml", "capability-decisions.yaml"], "catalog.product-card", ["SFB-COMP-002"], "replace only if previously generated"),
  entry(`${root}/Components/Catalog/ProductGalleryPlaceholder.razor`, "generated", "replace", ["ui-patterns.yaml"], "product.gallery", ["SFB-COMP-003"], "replace only if previously generated"),
  entry(`${root}/Components/Catalog/PurchasePanelPlaceholder.razor`, "generated", "patch", ["behaviors.yaml", "capability-decisions.yaml"], "product.purchase", ["SFB-COMMERCE-001"], "preserve Starter action binding"),
  entry(`${root}/Pages/Hybrid/Commerce/CartPage.razor`, "managed", "patch", ["design-tokens.yaml"], "cart.page", ["SFB-FALLBACK-001"], "theme fallback only"),
  entry(`${root}/Endpoints/StarterBffEndpoints.cs`, "protected", "skip", ["starter-generation.contract.yaml"], "none", ["SFB-PROTECTED-001"], "never edit"),
  entry(`${root}/StorefrontPackageVersions.props`, "protected", "skip", ["starter-generation.contract.yaml"], "none", ["SFB-PROTECTED-001"], "never edit"),
];

const plan = {
  schemaVersion: "1.0.0",
  artifactKind: "generation-plan",
  artifactId: "generation-plan.default",
  generatorVersion: "static",
  sourceHandoffPackageHash: "static",
  sourceHandoffReadinessHash: "static",
  sourceStarterContractHash: specHash,
  projectName,
  storeKey,
  generationMode: "static",
  generationOrder: ["generate-from-starter", "apply-visual-files"],
  sourceSpecHash: specHash,
  files,
  slots: [],
  assets: [],
  copyBlocks: [],
  tokens: [],
  warnings: [],
  blockedItems: [],
};

if (dryRun) {
  for (const file of files) {
    console.log(`${file.action.padEnd(7)} ${file.ownership.padEnd(9)} ${file.filePath}`);
  }
}

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${toYaml(plan)}\n`, "utf8");
mkdirSync(dirname(jsonOutput), { recursive: true });
writeFileSync(jsonOutput, stableJson(plan), "utf8");

function entry(filePath, ownership, action, sourceArtifactIds, expectedSlot, validationRuleIds, conflictBehavior) {
  const targetPath = filePath.startsWith(`${root}/`) ? filePath.slice(root.length + 1) : filePath;
  return {
    filePath,
    targetPath,
    ownership,
    action,
    allowedOperation: action,
    sourceArtifactIds,
    sourceHandoffArtifacts: sourceArtifactIds,
    sourceEvidenceReferences: [],
    expectedSlot,
    validationRuleIds,
    conflictBehavior,
    sourceSpecHash: specHash,
    generatedHash: sha(`${filePath}:${ownership}:${action}:${sourceArtifactIds.join(",")}`),
    rationale: "Static StorefrontBuilder default generation plan entry.",
  };
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
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
  return text.length === 0 ? "\"\"" : /^[a-zA-Z0-9_.\/:-]+$/.test(text) ? text : JSON.stringify(text);
}
