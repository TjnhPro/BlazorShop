#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import {
  buildManifestEntries,
  buildRegenerationReport,
  readPreviousManifest,
  scanProjectFiles,
  writeManifestYaml,
} from "./generated-file-manifest.mjs";

const projectRoot = readArg("--workspace-root") ?? readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const intentionalChangeItems = readListArg("--intentional-changes");
const intentionalChanges = intentionalChangeItems.includes("__all__")
  ? new Set(scanProjectFiles(projectRoot).map((file) => file.filePath))
  : new Set(intentionalChangeItems);
const output = `${projectRoot}/docs/storefront-analysis/generated-files.yaml`;
const report = `${projectRoot}/docs/storefront-analysis/regeneration-report.md`;

const previousEntries = readPreviousManifest(output);
const entries = buildManifestEntries(projectRoot, previousEntries, intentionalChanges);

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, writeManifestYaml(entries), "utf8");
writeFileSync(report, buildRegenerationReport(entries), "utf8");
console.log(`Updated multi-project generated file manifest at ${output}`);

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function readListArg(name) {
  const value = readArg(name);
  if (!value) {
    return [];
  }

  return value
    .split(",")
    .map((item) => item.trim().replaceAll("\\", "/"))
    .filter(Boolean);
}
