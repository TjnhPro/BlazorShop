#!/usr/bin/env node
import { createHash } from "node:crypto";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";

const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const output = `${projectRoot}/docs/storefront-analysis/asset-manifest.yaml`;
const placeholder = `${projectRoot}/wwwroot/assets/generated/asset-placeholder.svg`;
const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 640 640" role="img" aria-label="Replacement asset needed"><rect width="640" height="640" fill="#d9dfd8"/><path d="M96 448l128-128 96 96 96-128 128 160H96z" fill="#0b6b57"/><circle cx="438" cy="202" r="48" fill="#c94c2f"/></svg>\n`;
const checksum = createHash("sha256").update(svg).digest("hex");

mkdirSync(dirname(placeholder), { recursive: true });
writeFileSync(placeholder, svg, "utf8");

const manifest = `schemaVersion: 1.0.0
artifactKind: asset-manifest
artifactId: asset-manifest.generated-proof
licenseNotice: "Reference-site assets are evidence only; this manifest makes no production licensing claim."
assets:
  - assetId: placeholder-product-media
    sourceUrl: generated://storefront-builder/placeholder
    checksum: sha256:${checksum}
    contentType: image/svg+xml
    detectedUsage: product-gallery product-card hero-fallback
    normalizedFilename: asset-placeholder.svg
    duplicateOf: none
    allowedToCopy: true
    replacementNeeded: true
    replacementPath: wwwroot/assets/generated/asset-placeholder.svg
replacementList:
  - placeholder-product-media
rules:
  - Missing asset must not break build.
  - Manual asset replacement must not require code changes.
`;

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, manifest, "utf8");
console.log(`Generated asset manifest at ${output}`);

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
