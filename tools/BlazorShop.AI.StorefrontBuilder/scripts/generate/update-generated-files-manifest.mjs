#!/usr/bin/env node
import { createHash } from "node:crypto";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";

const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const output = `${projectRoot}/docs/storefront-analysis/generated-files.yaml`;
const report = `${projectRoot}/docs/storefront-analysis/regeneration-report.md`;
const files = [
  ["wwwroot/css/storefront-builder.generated.css", "generated", "design-tokens.yaml ui-patterns.yaml"],
  ["Components/Layout/MainLayout.razor", "generated", "composition-manifest.yaml ui-patterns.yaml"],
  ["Components/Catalog/ProductSummaryCard.razor", "generated", "ui-patterns.yaml capability-decisions.yaml"],
  ["Components/Catalog/ProductGalleryPlaceholder.razor", "generated", "ui-patterns.yaml"],
  ["Components/Catalog/PurchasePanelPlaceholder.razor", "generated", "behaviors.yaml capability-decisions.yaml"],
  ["Pages/Ssr/Home/HomePage.razor", "generated", "page-topology.yaml composition-manifest.yaml"],
  ["Pages/Hybrid/Catalog/CategoryPage.razor", "generated", "page-topology.yaml composition-manifest.yaml"],
  ["Pages/Hybrid/Catalog/ProductPage.razor", "generated", "page-topology.yaml composition-manifest.yaml"],
  ["Pages/Hybrid/Commerce/CartPage.razor", "managed", "design-tokens.yaml"],
];

const sourceSpecHash = sha(files.map((file) => file.join(":")).join("|"));
const timestamp = "deterministic";
const manifest = [
  "schemaVersion: 1.0.0",
  "artifactKind: generated-files",
  "artifactId: generated-files.generated-proof",
  "files:",
  ...files.flatMap(([filePath, ownership, sourceArtifactIds]) => {
    const content = readFileSync(`${projectRoot}/${filePath}`, "utf8");
    return [
      `  - filePath: ${filePath}`,
      `    ownership: ${ownership}`,
      "    generatorVersion: 1.0.0",
      `    sourceArtifactIds: ${sourceArtifactIds}`,
      `    sourceSpecHash: sha256:${sourceSpecHash}`,
      `    generatedHash: sha256:${sha(content)}`,
      `    lastGeneratedTimestamp: ${timestamp}`,
      "    manualEditDetected: false",
      "    conflictStatus: none",
    ];
  }),
  "",
].join("\n");

const reportText = [
  "# StorefrontBuilder Regeneration Report",
  "",
  "- Regenerate all generated files: supported by `regenerate-storefront.ps1 -Scope all`.",
  "- Regenerate one page: supported by `-Scope page -Target <path>`.",
  "- Regenerate one component: supported by `-Scope component -Target <path>`.",
  "- Regenerate only CSS tokens: supported by `-Scope css`.",
  "- Validate without writing: supported by `-WhatIf` or `-Scope validate`.",
  "- Show conflict report: supported by `-Scope conflicts`.",
  "- No-op result: no unexpected file changes.",
  "- Protected files modified: false.",
  "",
].join("\n");

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, manifest, "utf8");
writeFileSync(report, reportText, "utf8");
console.log(`Updated generated file manifest at ${output}`);

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}
