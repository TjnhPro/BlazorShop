#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, isAbsolute, join, resolve } from "node:path";

const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const planPath = resolve(readArg("--plan-json") ?? join(projectRoot, "docs/storefront-analysis/generation-plan.json"));
const summaryPath = resolve(readArg("--summary-output") ?? join(projectRoot, "docs/storefront-analysis/handoff-generation-summary.md"));
const placeholderManifestPath = resolve(readArg("--placeholder-manifest-output") ?? join(projectRoot, "docs/storefront-analysis/handoff-placeholders.json"));

if (!existsSync(planPath)) {
  fail("SFB-HANDOFF-GEN-001", `Generation plan is missing: ${planPath}`);
}

const planText = readFileSync(planPath, "utf8");
const plan = JSON.parse(planText);
if (plan.generationMode !== "handoff") {
  fail("SFB-HANDOFF-GEN-002", "Handoff project skeleton requires a handoff generation plan.");
}

const blockingItems = (plan.blockedItems ?? []).filter(item => String(item.severity ?? "").toLowerCase() === "blocking");
if (blockingItems.length > 0) {
  fail("SFB-HANDOFF-GEN-006", `Generation plan has blocking items: ${blockingItems.map(item => item.code ?? item.itemId).join(", ")}`);
}

const planHash = `sha256:${sha(planText.replace(/\r\n/g, "\n").replace(/\r/g, "\n"))}`;
const written = [];
const skipped = [];

ensureGeneratedCssLink();

for (const file of plan.files ?? []) {
  const isProtectedSkip = file.ownership === "protected" || file.action === "skip";
  const targetPath = normalizeTargetPath(file.targetPath, isProtectedSkip);
  if (file.declaresRoute === true) {
    fail("SFB-HANDOFF-GEN-005", `Planned file declares route ownership: ${targetPath}`);
  }

  const fullPath = resolve(projectRoot, targetPath);
  assertUnderProject(fullPath, targetPath);

  if (isProtectedSkip) {
    skipped.push({ targetPath, reason: "protected-or-skip" });
    continue;
  }

  if (!["create", "replace", "patch"].includes(file.allowedOperation ?? file.action)) {
    skipped.push({ targetPath, reason: `unsupported-operation-${file.allowedOperation ?? file.action}` });
    continue;
  }

  mkdirSync(dirname(fullPath), { recursive: true });
  if (targetPath.endsWith(".css")) {
    writeFileSync(fullPath, buildCssPlaceholder(file, plan), "utf8");
    written.push(record(file, targetPath, "css-placeholder"));
    continue;
  }

  if (targetPath.endsWith(".razor")) {
    if (existsSync(fullPath)) {
      const original = readFileSync(fullPath, "utf8");
      const updated = applyRazorMarkers(original, file);
      writeFileSync(fullPath, updated, "utf8");
      written.push(record(file, targetPath, updated === original ? "razor-placeholder-present" : "razor-placeholder-marker"));
    } else {
      writeFileSync(fullPath, buildRazorPlaceholder(file), "utf8");
      written.push(record(file, targetPath, "razor-placeholder-file"));
    }
    continue;
  }

  if (!existsSync(fullPath)) {
    writeFileSync(fullPath, `# StorefrontBuilder handoff placeholder\n\nPlan entry: ${file.id}\n`, "utf8");
    written.push(record(file, targetPath, "text-placeholder-file"));
  } else {
    skipped.push({ targetPath, reason: "existing-nonvisual-file" });
  }
}

const placeholderManifest = stableObject({
  schemaVersion: "1.0.0",
  artifactKind: "handoff-placeholders",
  artifactId: `handoff-placeholders.${plan.projectName}`,
  projectName: plan.projectName,
  storeKey: plan.storeKey,
  generationPlanHash: planHash,
  files: written.sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en")),
  skipped: skipped.sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en")),
});

mkdirSync(dirname(placeholderManifestPath), { recursive: true });
writeFileSync(placeholderManifestPath, `${JSON.stringify(placeholderManifest, null, 2)}\n`, "utf8");
writeFileSync(summaryPath, buildSummary(plan, planHash, written, skipped), "utf8");
console.log(`StorefrontBuilder handoff skeleton applied from ${planPath}`);

function ensureGeneratedCssLink() {
  const headPath = join(projectRoot, "Components/Layout/ApplicationHead.razor");
  if (!existsSync(headPath)) {
    return;
  }

  const content = readFileSync(headPath, "utf8");
  if (content.includes("css/storefront-builder.generated.css")) {
    return;
  }

  writeFileSync(
    headPath,
    content.replace(
      '<link rel="stylesheet" href="css/starter.css" />',
      '<link rel="stylesheet" href="css/starter.css" />\n<link rel="stylesheet" href="css/storefront-builder.generated.css" />'
    ),
    "utf8");
}

function applyRazorMarkers(content, file) {
  const marker = `storefront-builder-handoff-placeholder: ${file.id}`;
  let updated = content;
  if (!updated.includes(marker)) {
    updated = `${updated.trimEnd()}\n\n@* ${marker} | slots: ${(file.slots ?? []).join(",")} | plan-owned visual placeholder only *@\n`;
  }

  for (const slot of file.slots ?? []) {
    const slotClass = classForSlot(slot);
    if (slotClass && !updated.includes(slotClass)) {
      updated = addClassMarker(updated, slotClass, slot);
    }
  }

  return updated;
}

function addClassMarker(content, slotClass, slot) {
  if (slot === "layout.header") {
    return content.replace('class="starter-header"', `class="starter-header ${slotClass}"`);
  }

  if (slot === "layout.main-navigation") {
    return content.replace('<nav aria-label="Main navigation">', `<nav class="${slotClass}" aria-label="Main navigation">`);
  }

  if (slot === "layout.mobile-navigation" && !content.includes("sfb-mobile-nav")) {
    return content.replace("</header>", `<nav class="${slotClass}" aria-label="Mobile navigation"></nav>\n</header>`);
  }

  if (slot === "layout.cart-badge") {
    return content.replace("<span data-storefront-cart-badge", `<span class="${slotClass}" data-storefront-cart-badge`);
  }

  if (slot === "home.sections") {
    return content.replace("<h1>", `<h1 class="sfb-hero">`).replace('class="starter-section"', 'class="starter-section sfb-featured-grid"');
  }

  if (slot === "catalog.product-card") {
    return content.replace('class="starter-product-card"', 'class="starter-product-card sfb-product-card"');
  }

  if (slot === "catalog.filters") {
    return content.replace("<PlaceholderState", `<section class="sfb-catalog-toolbar" aria-label="Catalog controls"></section>\n<PlaceholderState`);
  }

  if (slot === "product.gallery") {
    return content.replace('class="starter-gallery-placeholder"', 'class="starter-gallery-placeholder sfb-product-gallery"');
  }

  if (slot === "product.information") {
    return content.replace("<article", '<article class="sfb-product-page"');
  }

  if (slot === "product.purchase") {
    return content.replace('class="starter-purchase-panel"', 'class="starter-purchase-panel sfb-product-purchase"').replace('class="starter-quantity-control"', 'class="starter-quantity-control sfb-quantity-control"');
  }

  if (["cart.page", "checkout.page", "account.shell", "system.error"].includes(slot)) {
    return content.replace("<h1>", '<h1 class="sfb-fallback-page">');
  }

  return content;
}

function buildRazorPlaceholder(file) {
  const slots = file.slots ?? [];
  return [
    `@* storefront-builder-handoff-placeholder: ${file.id} | slots: ${slots.join(",")} | plan-owned visual placeholder only *@`,
    `<section class="sfb-handoff-placeholder ${slots.map(classForSlot).filter(Boolean).join(" ")}" data-storefront-generated-placeholder data-storefront-slot="${slots.join(" ")}">`,
    "    <div class=\"sfb-handoff-placeholder__surface\"></div>",
    "</section>",
    "",
  ].join("\n");
}

function buildCssPlaceholder(file, plan) {
  const tokenGroups = (file.tokenGroups ?? plan.tokens?.map(item => item.tokenGroup) ?? []).join(", ");
  return [
    "/* StorefrontBuilder handoff visual placeholder.",
    `   Plan entry: ${file.id}`,
    `   Token groups: ${tokenGroups}`,
    "   Agent visual generation may replace this file within generated-owned boundaries. */",
    ":root {",
    "  --sfb-color-handoff-surface: #ffffff;",
    "  --sfb-color-handoff-ink: #172033;",
    "  --sfb-font-handoff-body: system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif;",
    "  --sfb-text-handoff-base: 1rem;",
    "  --sfb-space-handoff-3: 0.75rem;",
    "  --sfb-container-handoff-max: 72rem;",
    "  --sfb-border-width-handoff: 1px;",
    "  --sfb-radius-handoff: 0.5rem;",
    "  --sfb-shadow-handoff-soft: 0 8px 28px rgba(23, 32, 51, 0.08);",
    "  --sfb-motion-handoff-fast: 160ms;",
    "  --sfb-ease-handoff-standard: cubic-bezier(0.2, 0, 0, 1);",
    "}",
    ".sfb-handoff-placeholder {",
    "  font-family: var(--sfb-font-handoff-body);",
    "  color: var(--sfb-color-handoff-ink);",
    "  max-width: var(--sfb-container-handoff-max);",
    "}",
    "button.sfb-handoff-placeholder__button, input.sfb-handoff-placeholder__input {",
    "  border: var(--sfb-border-width-handoff) solid currentColor;",
    "  border-radius: var(--sfb-radius-handoff);",
    "}",
    ".starter-product-card, .sfb-handoff-placeholder__media {",
    "  aspect-ratio: 1 / 1;",
    "}",
    ":focus-visible {",
    "  outline: 2px solid currentColor;",
    "  outline-offset: 3px;",
    "}",
    "@media (max-width: 48rem) {",
    "  .sfb-handoff-placeholder {",
    "    padding: var(--sfb-space-handoff-3);",
    "  }",
    "}",
    "",
  ].join("\n");
}

function buildSummary(plan, planHash, writtenFiles, skippedFiles) {
  const warnings = plan.warnings ?? [];
  const warningLines = warnings.length === 0
    ? ["- No optional handoff warnings."]
    : warnings.map(item => `- ${item.code}: ${item.pageId ?? "project"} ${item.slotId ?? ""}`.trim());

  return [
    "# StorefrontBuilder Handoff Generation Summary",
    "",
    `- Project: ${plan.projectName}`,
    `- Store key: ${plan.storeKey}`,
    `- Generator version: ${plan.generatorVersion}`,
    `- Handoff package hash: ${plan.sourceHandoffPackageHash}`,
    `- Handoff readiness hash: ${plan.sourceHandoffReadinessHash}`,
    `- Starter contract hash: ${plan.sourceStarterContractHash}`,
    `- Generation plan hash: ${planHash}`,
    `- Planned files: ${(plan.files ?? []).length}`,
    `- Placeholder writes: ${writtenFiles.length}`,
    `- Protected/skipped files: ${skippedFiles.length}`,
    "",
    "## Warnings",
    "",
    ...warningLines,
    "",
    "## Placeholder Files",
    "",
    ...writtenFiles.sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en")).map(item => `- ${item.targetPath}: ${item.operation} (${item.planEntryId})`),
    "",
    "## Skipped Files",
    "",
    ...(skippedFiles.length === 0 ? ["- None."] : skippedFiles.map(item => `- ${item.targetPath}: ${item.reason}`)),
    "",
  ].join("\n");
}

function record(file, targetPath, operation) {
  return {
    targetPath,
    operation,
    planEntryId: file.id,
    ownership: file.ownership,
    slots: file.slots ?? [],
    sourceHandoffArtifacts: file.sourceHandoffArtifacts ?? [],
    sourceEvidenceReferences: file.sourceEvidenceReferences ?? [],
    checksum: `sha256:${sha(`${targetPath}:${operation}:${file.id}:${(file.slots ?? []).join(",")}`)}`,
  };
}

function classForSlot(slot) {
  return {
    "layout.header": "sfb-shell-header",
    "layout.main-navigation": "sfb-main-nav",
    "layout.mobile-navigation": "sfb-mobile-nav",
    "layout.cart-badge": "sfb-cart-badge",
    "home.sections": "sfb-hero",
    "catalog.product-card": "sfb-product-card",
    "catalog.filters": "sfb-catalog-toolbar",
    "product.gallery": "sfb-product-gallery",
    "product.information": "sfb-product-page",
    "product.purchase": "sfb-product-purchase",
    "cart.page": "sfb-fallback-page",
    "checkout.page": "sfb-fallback-page",
    "account.shell": "sfb-fallback-page",
    "system.error": "sfb-fallback-page",
  }[slot] ?? "";
}

function normalizeTargetPath(targetPath, allowProtected = false) {
  const normalized = String(targetPath ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
  if (!normalized || isAbsolute(normalized) || normalized.includes(":") || normalized.startsWith("../") || normalized.includes("/../")) {
    fail("SFB-HANDOFF-GEN-003", `Unsafe target path in generation plan: ${targetPath}`);
  }

  if (!allowProtected
    && (/(^|\/)(BlazorShop\.Storefront\.Starter|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Client|BlazorShop\.Storefront\.V2)(\/|$)/.test(normalized)
    || normalized === "StorefrontPackageVersions.props"
    || normalized === "starter-generation.contract.yaml")) {
    fail("SFB-HANDOFF-GEN-004", `Generation plan targets protected file or package zone: ${targetPath}`);
  }

  return normalized;
}

function assertUnderProject(fullPath, targetPath) {
  const root = resolve(projectRoot);
  if (fullPath !== root && !fullPath.startsWith(`${root}\\`) && !fullPath.startsWith(`${root}/`)) {
    fail("SFB-HANDOFF-GEN-003", `Target path escapes generated project: ${targetPath}`);
  }
}

function stableObject(value) {
  if (Array.isArray(value)) {
    return value.map(stableObject);
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(Object.keys(value).sort().map(key => [key, stableObject(value[key])]));
  }

  return value;
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}

function fail(code, message) {
  throw new Error(`[${code}] ${message}`);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
