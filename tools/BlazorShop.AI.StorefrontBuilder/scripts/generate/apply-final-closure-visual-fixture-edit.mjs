#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, isAbsolute, join, resolve } from "node:path";

if (process.argv.includes("--help") || process.argv.includes("-h")) {
  console.log(`Usage: node apply-final-closure-visual-fixture-edit.mjs --project-root <generated-project-root> [options]

Options:
  --project-root <path>   Generated storefront project root.
  --operation-id <id>     Closure operation ID. Defaults to phase4-12-final-closure-pilot.
  --help, -h              Show this help text.`);
  process.exit(0);
}

const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const operationId = readArg("--operation-id") ?? "phase4-12-final-closure-pilot";
const analysisRoot = join(projectRoot, "docs/storefront-analysis");
const taskPackageManifestPath = join(analysisRoot, "agent-task-package", "manifest.json");
const generationPlanPath = join(analysisRoot, "generation-plan.json");
const metadataPath = join(analysisRoot, "metadata.yaml");
const visualPlanPath = join(analysisRoot, "visual-plan.json");
const checklistPath = join(analysisRoot, "visual-implementation-checklist.json");
const implementationReportPath = join(analysisRoot, "visual-implementation-report.json");
const checkpointPath = join(analysisRoot, "visual-checkpoints", operationId, "visual-checkpoint.json");

if (!existsSync(projectRoot)) {
  fail("SFB-FINAL-CLOSURE-EDIT-001", `Generated project root does not exist: ${projectRoot}`);
}

for (const path of [taskPackageManifestPath, generationPlanPath, metadataPath]) {
  if (!existsSync(path)) {
    fail("SFB-FINAL-CLOSURE-EDIT-002", `Required handoff generation artifact is missing: ${path}`);
  }
}

const taskPackage = readJson(taskPackageManifestPath);
if (taskPackage.artifactKind !== "agent-visual-task-package") {
  fail("SFB-FINAL-CLOSURE-EDIT-003", `Task package artifactKind must be agent-visual-task-package, but was '${taskPackage.artifactKind}'.`);
}

const generationPlan = readJson(generationPlanPath);
if (generationPlan.generationMode !== "handoff") {
  fail("SFB-FINAL-CLOSURE-EDIT-004", `Generation plan mode must be handoff, but was '${generationPlan.generationMode}'.`);
}

const allowedOutputs = (taskPackage.allowedOutputFiles ?? []).map(normalizeAllowedOutput);
const generatedCandidate = allowedOutputs.find(file =>
  file.ownership === "generated" &&
  file.targetPath.endsWith(".razor") &&
  !file.targetPath.endsWith("PurchasePanelPlaceholder.razor") &&
  existsSync(join(projectRoot, file.targetPath)) &&
  hasGeneratedVisualClassMarker(join(projectRoot, file.targetPath)));
const selected = generatedCandidate ?? allowedOutputs.find(file =>
  file.targetPath.endsWith(".razor") &&
  existsSync(join(projectRoot, file.targetPath)) &&
  hasGeneratedVisualClassMarker(join(projectRoot, file.targetPath)));
if (!selected) {
  fail("SFB-FINAL-CLOSURE-EDIT-005", "No allowed generated Razor visual output exists for deterministic closure edit.");
}

const selectedPath = selected.targetPath;
const selectedFullPath = join(projectRoot, selectedPath);
assertSafeProjectPath(selectedPath, selectedFullPath);
assertNotProtectedClosureTarget(selectedPath);
const beforeContent = readFileSync(selectedFullPath, "utf8");
if (/^\s*@page\s+/m.test(beforeContent)) {
  fail("SFB-FINAL-CLOSURE-EDIT-006", `Selected closure edit file declares a route: ${selectedPath}`);
}

const scope = normalizeUnique([
  selectedPath,
  ...allowedOutputs.map(file => file.targetPath).filter(path => existsSync(join(projectRoot, path))),
]);
const preHashes = hashFiles(scope);
const changedContent = applyDeterministicVisualOnlyEdit(beforeContent, selectedPath);
writeFileSync(selectedFullPath, changedContent, "utf8");
const postHashes = hashFiles(scope);
const beforeSha = preHashes.find(item => item.filePath === selectedPath)?.sha256 ?? "missing";
const afterSha = postHashes.find(item => item.filePath === selectedPath)?.sha256 ?? "missing";
if (beforeSha === afterSha) {
  fail("SFB-FINAL-CLOSURE-EDIT-007", `Deterministic closure edit did not change ${selectedPath}.`);
}

const projectName = readSimpleYamlValue(readFileSync(metadataPath, "utf8"), "projectName") ?? taskPackage.projectName ?? generationPlan.projectName;
const storeKey = readSimpleYamlValue(readFileSync(metadataPath, "utf8"), "storeKey") ?? taskPackage.storeKey ?? generationPlan.storeKey ?? "sample";
const taskPackageHash = normalizedFileHash(taskPackageManifestPath);
const generationPlanHash = normalizedFileHash(generationPlanPath);
const handoffHash = generationPlan.sourceHandoffPackageHash?.startsWith("sha256:")
  ? generationPlan.sourceHandoffPackageHash
  : `sha256:${generationPlan.sourceHandoffPackageHash ?? "unknown"}`;

const coverage = [{ pageId: "home", viewports: ["desktop", "tablet", "mobile"] }];
const visualPlan = stableObject({
  schemaVersion: "0.1.0",
  operationId,
  projectName,
  storeKey,
  handoffHash,
  generationPlanHash,
  taskPackageHash,
  pages: [{ id: "home", route: "/", priority: 1 }],
  pageViewportCoverage: coverage,
  visualSlots: (selected.slots ?? []).map(slot => ({
    id: slot,
    pageId: slot.startsWith("home.") ? "home" : "shared",
    targetFiles: [selectedPath],
    status: "completed",
  })),
  allowedFiles: allowedOutputs.map(file => file.targetPath).sort((a, b) => a.localeCompare(b, "en")),
  plannedGeneratedOwnedFiles: allowedOutputs.filter(file => file.ownership === "generated" || file.visualShellOnly).map(file => file.targetPath).sort((a, b) => a.localeCompare(b, "en")),
  protectedFiles: [
    "Program.cs",
    "appsettings.json",
    "BlazorShop.Storefront.Starter",
    "BlazorShop.Storefront.Presentation",
    "BlazorShop.Storefront.Runtime",
    "BlazorShop.Storefront.Client",
  ],
  implementationOrder: [selectedPath],
  risks: [],
  blockers: [],
});

writeJson(visualPlanPath, visualPlan);
const visualPlanHash = normalizedFileHash(visualPlanPath);
const checklist = stableObject({
  schemaVersion: "0.1.0",
  checklistId: `checklist-${operationId}`,
  sourceVisualPlanHash: visualPlanHash,
  fileTasks: [{
    filePath: selectedPath,
    taskIds: selected.slots?.length ? selected.slots : [selected.planEntryId],
    status: "completed",
  }],
  acceptanceChecks: [
    "No @page directives are added.",
    "No transport, BFF endpoint, DTO, auth, SEO, cart, checkout, payment, or order behavior is added.",
    "Exactly one allowed generated visual source file is changed.",
  ],
  requiredScreenshots: ["desktop", "tablet", "mobile"],
  forbiddenEdits: visualPlan.protectedFiles,
});
writeJson(checklistPath, checklist);
const checklistHash = normalizedFileHash(checklistPath);
const checkpoint = stableObject({
  schemaVersion: "0.1.0",
  checkpointId: `checkpoint-${operationId}`,
  operationId,
  visualPlanHash,
  checklistHash,
  preEditSnapshotHash: hashSnapshot(preHashes),
  postEditSnapshotHash: hashSnapshot(postHashes),
  changedFiles: [selectedPath],
  unexpectedFiles: [],
  sourceTreeSnapshotScope: scope,
  preEditFileHashes: preHashes,
  postEditFileHashes: postHashes,
  diffSummary: [{
    filePath: selectedPath,
    changeType: "modified",
    summary: "Added deterministic Phase 4.12 visual-only proof class to an allowed generated Razor component.",
  }],
});
writeJson(checkpointPath, checkpoint);

const implementationReport = stableObject({
  schemaVersion: "0.1.0",
  operationId,
  checkpointPath: relativeToProject(checkpointPath),
  beforeSnapshotHash: checkpoint.preEditSnapshotHash,
  afterSnapshotHash: checkpoint.postEditSnapshotHash,
  changedFiles: [selectedPath],
  fileChanges: [{
    filePath: selectedPath,
    beforeSha256: beforeSha,
    afterSha256: afterSha,
  }],
  recorderResultPath: "docs/storefront-analysis/agent-written-files.json",
  boundaryResult: {
    status: "pending",
    command: "node tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs",
  },
  buildResult: {
    status: "pending",
    command: `dotnet build ${projectName}.csproj --no-restore`,
  },
  unresolvedItems: [],
});
writeJson(implementationReportPath, implementationReport);

console.log(`Applied deterministic Phase 4.12 closure edit to ${selectedPath}`);
console.log(`Visual checkpoint: ${checkpointPath}`);

function applyDeterministicVisualOnlyEdit(content, targetPath) {
  if (content.includes("sfb-phase412-proof")) {
    return content;
  }

  const classMatch = content.match(/class="([^"]*?\bsfb-[^"]*)"/);
  if (classMatch) {
    return content.replace(classMatch[0], `class="${classMatch[1]} sfb-phase412-proof"`);
  }

  fail("SFB-FINAL-CLOSURE-EDIT-008", `Selected file has no generated visual class marker to edit safely: ${targetPath}`);
}

function hasGeneratedVisualClassMarker(path) {
  return /class="[^"]*?\bsfb-[^"]*"/.test(readFileSync(path, "utf8"));
}

function hashFiles(paths) {
  return normalizeUnique(paths).map(filePath => {
    const fullPath = join(projectRoot, filePath);
    assertSafeProjectPath(filePath, fullPath);
    if (!existsSync(fullPath)) {
      return { filePath, sha256: "missing" };
    }

    return { filePath, sha256: normalizedFileHash(fullPath) };
  });
}

function normalizeAllowedOutput(file) {
  const targetPath = normalizeTargetPath(file?.targetPath);
  return {
    targetPath,
    planEntryId: String(file.planEntryId ?? targetPath),
    ownership: String(file.ownership ?? ""),
    visualShellOnly: file.visualShellOnly === true,
    slots: file.slots ?? [],
  };
}

function assertSafeProjectPath(targetPath, fullPath) {
  const root = resolve(projectRoot);
  const full = resolve(fullPath);
  if (full !== root && !full.startsWith(`${root}\\`) && !full.startsWith(`${root}/`)) {
    fail("SFB-FINAL-CLOSURE-EDIT-009", `Path escapes generated project: ${targetPath}`);
  }
}

function assertNotProtectedClosureTarget(targetPath) {
  if (targetPath === "Program.cs" || targetPath === "appsettings.json" || targetPath.endsWith(".csproj") || isProtectedPackagePath(targetPath)) {
    fail("SFB-FINAL-CLOSURE-EDIT-011", `Selected closure edit file is protected and cannot be used as final visual proof: ${targetPath}`);
  }
}

function isProtectedPackagePath(path) {
  return /(^|\/)(BlazorShop\.Storefront\.Starter|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Client|BlazorShop\.Storefront\.Browser|BlazorShop\.Storefront\.Components)(\/|$)/.test(path);
}

function normalizeTargetPath(path) {
  const normalized = String(path ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
  if (!normalized || isAbsolute(normalized) || normalized.includes(":") || normalized.startsWith("../") || normalized.includes("/../")) {
    fail("SFB-FINAL-CLOSURE-EDIT-010", `Unsafe path in task package: ${path}`);
  }

  return normalized;
}

function relativeToProject(path) {
  const root = resolve(projectRoot);
  const full = resolve(path);
  return full.startsWith(`${root}\\`) || full.startsWith(`${root}/`)
    ? full.slice(root.length + 1).replaceAll("\\", "/")
    : full.replaceAll("\\", "/");
}

function normalizedFileHash(path) {
  return `sha256:${sha(normalizeText(readFileSync(path, "utf8")))}`;
}

function hashSnapshot(items) {
  return `sha256:${sha(items.map(item => `${item.filePath}\n${item.sha256}`).join("\n"))}`;
}

function normalizeText(value) {
  return value.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function normalizeUnique(items) {
  return [...new Set((items ?? []).map(normalizeTargetPath))].sort((a, b) => a.localeCompare(b, "en"));
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

function writeJson(path, value) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(stableObject(value), null, 2)}\n`, "utf8");
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function readSimpleYamlValue(text, key) {
  const match = text.match(new RegExp(`^\\s*${escapeRegex(key)}:\\s*(.*?)\\s*$`, "m"));
  return match ? match[1].trim().replace(/^"|"$/g, "") : null;
}

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
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
