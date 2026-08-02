#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

if (process.argv.includes("--help") || process.argv.includes("-h")) {
  console.log(`Usage: node repair-visual-generation.mjs [options]

Options:
  --project-root <path>       Generated storefront project root.
  --failure-report <path>     Browser/build/boundary failure report to classify.
  --max-attempts <number>     Maximum bounded repair attempts, default 2.
  --help, -h                  Show this help text.

Scope:
  Bounded helper only. It may repair generated-owned visual files from the agent task package.
  It rejects @page route additions, HttpClient/fetch transport, /api/storefront/stores calls,
  business logic, auth/session changes, seo changes, and protected descriptor edits.`);
  process.exit(0);
}

const scriptDir = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const failureReportPath = resolve(readArg("--failure-report") ?? join(projectRoot, "docs/storefront-analysis/visual-qa-report.md"));
const maxAttempts = Number.parseInt(readArg("--max-attempts") ?? "2", 10);
const historyPath = join(projectRoot, "docs/storefront-analysis/repair-history.md");
const planPath = join(projectRoot, "docs/storefront-analysis/generation-plan.json");
const taskPackagePath = join(projectRoot, "docs/storefront-analysis/agent-task-package/manifest.json");

if (!existsSync(planPath)) {
  fail("SFB-REPAIR-000", "Handoff generation plan is required before repair.");
}

if (!existsSync(failureReportPath)) {
  fail("SFB-REPAIR-001", `Failure report is missing: ${failureReportPath}`);
}

if (!existsSync(taskPackagePath)) {
  fail("SFB-REPAIR-002", "Agent task package manifest is required before repair.");
}

const planText = readFileSync(planPath, "utf8");
const planHashBefore = sha(normalizeText(planText));
const plan = JSON.parse(planText);
const taskPackage = readJson(taskPackagePath);
const allowedByPath = new Map((taskPackage.allowedOutputFiles ?? []).map(file => [normalizePath(file.targetPath), file]));
const failureText = readFileSync(failureReportPath, "utf8");
const priorAttempts = countPriorAttempts(historyPath);

if (priorAttempts >= maxAttempts) {
  appendHistory({
    status: "manual-blocker",
    failureSource: relativeFailurePath(),
    failingFile: "none",
    planEntryId: "none",
    attemptedFix: "none",
    result: "max-attempts-exceeded",
    remainingBlockers: [`Repair stopped after ${priorAttempts} attempt(s).`],
  });
  process.exitCode = 3;
  console.error(`[SFB-REPAIR-020] Repair stopped after ${priorAttempts} attempt(s); manual review required.`);
  process.exit();
}

const blocker = classifyManualBlocker(failureText);
if (blocker) {
  appendHistory({
    status: "manual-blocker",
    failureSource: relativeFailurePath(),
    failingFile: blocker.filePath ?? "unknown",
    planEntryId: "none",
    attemptedFix: "none",
    result: blocker.code,
    remainingBlockers: [blocker.message],
  });
  process.exitCode = 2;
  console.error(`[${blocker.code}] ${blocker.message}`);
  process.exit();
}

const repair = chooseRepair(failureText);
if (!repair) {
  appendHistory({
    status: "manual-blocker",
    failureSource: relativeFailurePath(),
    failingFile: "unknown",
    planEntryId: "none",
    attemptedFix: "none",
    result: "unsupported-failure",
    remainingBlockers: ["Failure output did not match a bounded visual repair pattern."],
  });
  process.exitCode = 2;
  console.error("[SFB-REPAIR-021] Failure output did not match a bounded visual repair pattern.");
  process.exit();
}

assertAllowedRepair(repair.targetPath);
const fullPath = join(projectRoot, repair.targetPath);
mkdirSync(dirname(fullPath), { recursive: true });
const before = existsSync(fullPath) ? readFileSync(fullPath, "utf8") : "";
writeFileSync(fullPath, repair.apply(before), "utf8");

const recorder = spawnSync(
  "node",
  [
    join(scriptDir, "..", "generate", "record-agent-visual-writes.mjs"),
    "--project-root",
    projectRoot,
    "--written-files",
    repair.targetPath,
  ],
  { encoding: "utf8" });

if (recorder.status !== 0) {
  writeFileSync(fullPath, before, "utf8");
  appendHistory({
    status: "manual-blocker",
    failureSource: relativeFailurePath(),
    failingFile: repair.targetPath,
    planEntryId: repair.planEntryId,
    attemptedFix: repair.description,
    result: "repair-validation-failed",
    remainingBlockers: [recorder.stdout + recorder.stderr],
  });
  process.exitCode = recorder.status ?? 1;
  console.error(recorder.stdout + recorder.stderr);
  process.exit();
}

const planHashAfter = sha(normalizeText(readFileSync(planPath, "utf8")));
if (planHashAfter !== planHashBefore) {
  writeFileSync(fullPath, before, "utf8");
  fail("SFB-REPAIR-030", "Repair changed generation-plan.json; re-plan explicitly instead.");
}

appendHistory({
  status: "applied",
  failureSource: relativeFailurePath(),
  failingFile: repair.targetPath,
  planEntryId: repair.planEntryId,
  attemptedFix: repair.description,
  result: "applied; rerun failed build/boundary/visual proof",
  remainingBlockers: ["Validation output after repair has not been rerun by this bounded repair step."],
});

console.log(`StorefrontBuilder repair applied to ${repair.targetPath}. Rerun the failed proof.`);

function classifyManualBlocker(text) {
  const blockers = [
    [/StorefrontPackageVersions\.props|protected file|protected generated file/i, "SFB-REPAIR-010", "Protected-file repair requires manual foundation scope review."],
    [/@page|route declaration/i, "SFB-REPAIR-011", "Route declarations are outside generated visual repair scope."],
    [/HttpClient|fetch\(|\/api\/storefront\/stores\/|CommerceNodeBaseUrl/i, "SFB-REPAIR-012", "Transport or Commerce Node calls are outside generated visual repair scope."],
    [/PlaceOrder|CapturePayment|ValidateCheckout|ValidateCart|ExpectedCartVersion|accessToken|refreshToken|seo|descriptor/i, "SFB-REPAIR-013", "Business/auth/seo or descriptor repair is outside generated visual repair scope."],
  ];

  for (const [pattern, code, message] of blockers) {
    if (pattern.test(text)) {
      return { code, message };
    }
  }

  return null;
}

function chooseRepair(text) {
  if (/Generated handoff CSS is not linked|No readable stylesheet rules|Horizontal overflow/i.test(text)) {
    const file = findPlannedFileByPath("wwwroot/css/storefront-builder.generated.css");
    return {
      targetPath: file.targetPath,
      planEntryId: file.id,
      description: "append bounded responsive CSS repair rules",
      apply: content => `${content.trimEnd()}\n\n/* StorefrontBuilder repair: bounded visual layout stabilization. */\nhtml, body { max-width: 100%; overflow-x: clip; }\n.sfb-handoff-placeholder, .sfb-product-gallery, .sfb-product-card { max-width: 100%; }\n@media (max-width: 48rem) { .sfb-shell-header, .sfb-product-page { min-width: 0; } }\n`,
    };
  }

  const slotMatch = text.match(/Required handoff slot '([^']+)' is not visible/i);
  if (slotMatch) {
    const slotId = slotMatch[1];
    const file = findPlannedFileBySlot(slotId);
    return {
      targetPath: file.targetPath,
      planEntryId: file.id,
      description: `append bounded missing-slot marker for ${slotId}`,
      apply: content => appendSlotMarkup(content, slotId, file),
    };
  }

  return null;
}

function appendSlotMarkup(content, slotId, file) {
  const marker = `StorefrontBuilder repair: ${slotId}`;
  if (content.includes(marker)) {
    return content;
  }

  if (slotId === "product.purchase") {
    return `${content.trimEnd()}\n\n<section class="sfb-product-purchase" data-storefront-product-purchase>\n    <button type="button" data-storefront-command="cart.add-line" data-storefront-product-purchase-submit>Repair placeholder</button>\n    <input data-storefront-purchase-quantity value="1" />\n</section>\n@* ${marker} | ${file.id} *@\n`;
  }

  return `${content.trimEnd()}\n\n<section class="${classForSlot(slotId)}" data-storefront-slot="${slotId}"></section>\n@* ${marker} | ${file.id} *@\n`;
}

function findPlannedFileByPath(targetPath) {
  const normalized = normalizePath(targetPath);
  const file = (plan.files ?? []).find(item => normalizePath(item.targetPath) === normalized);
  if (!file) {
    fail("SFB-REPAIR-022", `No planned generated file found for ${targetPath}.`);
  }

  return { ...file, targetPath: normalized };
}

function findPlannedFileBySlot(slotId) {
  const file = (plan.files ?? []).find(item => (item.slots ?? []).includes(slotId) && isRepairable(item));
  if (!file) {
    fail("SFB-REPAIR-023", `No repairable planned generated file found for slot ${slotId}.`);
  }

  return { ...file, targetPath: normalizePath(file.targetPath) };
}

function assertAllowedRepair(targetPath) {
  const normalized = normalizePath(targetPath);
  const allowed = allowedByPath.get(normalized);
  if (!allowed) {
    fail("SFB-REPAIR-024", `Repair target is outside agent allowed outputs: ${normalized}`);
  }

  if (!isRepairable({ ownership: allowed.ownership, visualShellOnly: allowed.visualShellOnly, targetPath: normalized })) {
    fail("SFB-REPAIR-025", `Repair target is not generated-owned visual scope: ${normalized}`);
  }
}

function isRepairable(file) {
  return (file.ownership === "generated" || file.visualShellOnly === true)
    && !/StorefrontPackageVersions\.props|\.csproj$|^Program\.cs$|appsettings\.json$/i.test(normalizePath(file.targetPath));
}

function appendHistory(entry) {
  const lines = existsSync(historyPath)
    ? readFileSync(historyPath, "utf8").trimEnd().split(/\r?\n/)
    : ["# StorefrontBuilder Repair History", ""];

  lines.push(
    "",
    `## Attempt ${countPriorAttempts(historyPath) + 1}`,
    "",
    `- timestamp: ${new Date().toISOString()}`,
    `- status: ${entry.status}`,
    `- failure source: ${entry.failureSource}`,
    `- failing file: ${entry.failingFile}`,
    `- plan entry id: ${entry.planEntryId}`,
    `- attempted fix: ${entry.attemptedFix}`,
    `- result: ${entry.result}`,
    "- remaining blockers:",
    ...entry.remainingBlockers.map(item => `  - ${singleLine(item)}`),
    "",
  );

  mkdirSync(dirname(historyPath), { recursive: true });
  writeFileSync(historyPath, `${lines.join("\n").trimEnd()}\n`, "utf8");
}

function countPriorAttempts(path) {
  if (!existsSync(path)) {
    return 0;
  }

  return (readFileSync(path, "utf8").match(/^## Attempt /gm) ?? []).length;
}

function classForSlot(slotId) {
  return {
    "layout.header": "sfb-shell-header",
    "layout.footer": "sfb-footer",
    "layout.main-navigation": "sfb-main-nav",
    "layout.mobile-navigation": "sfb-mobile-nav",
    "home.sections": "sfb-hero",
    "catalog.product-card": "sfb-product-card",
    "catalog.filters": "sfb-catalog-toolbar",
    "product.gallery": "sfb-product-gallery",
    "product.information": "sfb-product-page",
    "cart.page": "sfb-fallback-page",
    "checkout.page": "sfb-fallback-page",
    "account.shell": "sfb-fallback-page",
    "system.error": "sfb-fallback-page",
  }[slotId] ?? "sfb-handoff-placeholder";
}

function relativeFailurePath() {
  return failureReportPath.startsWith(`${projectRoot}\\`) || failureReportPath.startsWith(`${projectRoot}/`)
    ? failureReportPath.slice(projectRoot.length + 1).replaceAll("\\", "/")
    : failureReportPath.replaceAll("\\", "/");
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function normalizePath(value) {
  return String(value ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
}

function normalizeText(value) {
  return value.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function singleLine(value) {
  return String(value ?? "").replace(/\s+/g, " ").trim().slice(0, 500);
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
