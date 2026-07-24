#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";

const projectRoot = readArg("--project-root") ?? "BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo";
const url = readArg("--url") ?? "https://reference.example";
const output = `${projectRoot}/docs/storefront-analysis/review-summary.md`;

const content = `# StorefrontBuilder Review Summary

Reference URL: ${url}

## Visual Decision Summary

- Shell, home, catalog, and product pages use target-inspired generated presentation.
- Cart, checkout, and account remain Starter fallback pages themed with generated tokens.

## Unsupported Feature List

- Wishlist
- Product reviews

## Hidden Target Feature List

- Wishlist controls
- Review summary widgets

## Starter Fallback List

- Cart
- Checkout
- Account
- System/error states

## Asset Replacement List

- placeholder-product-media in asset-manifest.yaml

## AI Inference Review List

- Review low-confidence design tokens and inferred unsupported capability decisions.

## Manual Tuning Checklist

- Replace placeholder assets.
- Review typography and spacing against brand needs.
- Confirm hidden feature decisions with store owner.
- Run validate-only after manual changes.
`;

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, content, "utf8");
console.log(`Wrote review artifact ${output}`);

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
