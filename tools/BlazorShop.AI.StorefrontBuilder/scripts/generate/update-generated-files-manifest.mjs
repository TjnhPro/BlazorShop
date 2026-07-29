#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import {
  buildManifestEntries,
  buildRegenerationReport,
  readPreviousManifest,
  writeManifestYaml,
} from "./generated-file-manifest.mjs";

const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const output = `${projectRoot}/docs/storefront-analysis/generated-files.yaml`;
const report = `${projectRoot}/docs/storefront-analysis/regeneration-report.md`;

const previousEntries = readPreviousManifest(output);
const entries = buildManifestEntries(projectRoot, previousEntries);

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, writeManifestYaml(entries), "utf8");
writeFileSync(report, buildRegenerationReport(entries), "utf8");
console.log(`Updated generated file manifest at ${output}`);

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
