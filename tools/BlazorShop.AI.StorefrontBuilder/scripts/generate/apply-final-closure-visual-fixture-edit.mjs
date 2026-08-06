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
const checklistTodoPath = join(analysisRoot, "visual-implementation-checklist.todo.md");
const visualPlanSummaryPath = join(analysisRoot, "visual-plan-summary.md");
const implementationReportPath = join(analysisRoot, "visual-implementation-report.json");
const implementationReportMarkdownPath = join(analysisRoot, "visual-implementation-report.md");
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

const projectName = readSimpleYamlValue(readFileSync(metadataPath, "utf8"), "projectName") ?? taskPackage.projectName ?? generationPlan.projectName;
const storeKey = readSimpleYamlValue(readFileSync(metadataPath, "utf8"), "storeKey") ?? taskPackage.storeKey ?? generationPlan.storeKey ?? "sample";
const taskPackageHash = normalizedFileHash(taskPackageManifestPath);
const generationPlanHash = normalizedFileHash(generationPlanPath);
const handoffHash = generationPlan.sourceHandoffPackageHash?.startsWith("sha256:")
  ? generationPlan.sourceHandoffPackageHash
  : `sha256:${generationPlan.sourceHandoffPackageHash ?? "unknown"}`;

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
const selectedTarget = fileTargetFromAllowedOutput(selected, projectName);
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

const coverage = [{ pageId: "home", viewports: ["desktop", "tablet", "mobile"] }];
const allowedFileTargets = allowedOutputs
  .map(file => fileTargetFromAllowedOutput(file, projectName))
  .sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
const protectedFileTargets = normalizeProtectedTargets(taskPackage, projectName);
const visualPlan = stableObject({
  schemaVersion: "0.1.0",
  operationId,
  projectName,
  storeKey,
  projects: taskPackage.projects ?? {
    server: {
      name: projectName,
      rootPath: ".",
      projectPath: `${projectName}.csproj`,
    },
    wasm: {
      name: `${projectName}.WASM`,
      rootPath: `${projectName}.WASM`,
      projectPath: `${projectName}.WASM/${projectName}.WASM.csproj`,
    },
  },
  handoffHash,
  generationPlanHash,
  taskPackageHash,
  pages: [{ id: "home", route: "/", priority: 1 }],
  pageViewportCoverage: coverage,
  visualSlots: (selected.slots?.length ? selected.slots : [selected.planEntryId]).map(slot => ({
    id: slot,
    pageId: slot.startsWith("home.") ? "home" : "shared",
    targetProject: selectedTarget.targetProject,
    targetFiles: [selectedPath],
    status: "planned",
  })),
  allowedFiles: allowedOutputs.map(file => file.targetPath).sort((a, b) => a.localeCompare(b, "en")),
  allowedFileTargets,
  plannedGeneratedOwnedFiles: allowedOutputs.filter(file => file.ownership === "generated" || file.visualShellOnly).map(file => file.targetPath).sort((a, b) => a.localeCompare(b, "en")),
  protectedFiles: protectedFileTargets.map(file => file.targetPath),
  protectedFileTargets,
  implementationOrder: [selectedPath],
  risks: [],
  blockers: [],
});

writeJson(visualPlanPath, visualPlan);
writeMarkdown(visualPlanSummaryPath, [
  "# Storefront Visual Plan Summary",
  "",
  `Operation: \`${operationId}\``,
  `Project: \`${projectName}\``,
  `Store key: \`${storeKey}\``,
  `Handoff hash: \`${handoffHash}\``,
  "",
  "Planned generated-owned files:",
  ...visualPlan.plannedGeneratedOwnedFiles.map(file => `- \`${file}\``),
  "",
]);
const visualPlanHash = normalizedFileHash(visualPlanPath);
const checklist = stableObject({
  schemaVersion: "0.1.0",
  checklistId: `checklist-${operationId}`,
  sourceVisualPlanHash: visualPlanHash,
  fileTasks: [{
    filePath: selectedPath,
    project: selectedTarget.targetProject,
    projectRelativePath: selectedTarget.projectRelativePath,
    taskIds: selected.slots?.length ? selected.slots : [selected.planEntryId],
    status: "completed",
    notes: "Deterministic visual-only closure proof edit applied to an allowed generated file.",
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
writeMarkdown(checklistTodoPath, [
  "# Storefront Visual Implementation Checklist",
  "",
  `- [x] ${selectedPath}`,
  "- [x] No route, transport, auth, SEO, cart, checkout, payment, or order behavior changed.",
  "- [x] Visual write recorder is required after checkpoint creation.",
  "",
]);
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
  changedFileTargets: [{
    filePath: selectedPath,
    project: selectedTarget.targetProject,
    projectRelativePath: selectedTarget.projectRelativePath,
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
writeMarkdown(implementationReportMarkdownPath, [
  "# Storefront Visual Implementation Report",
  "",
  `Operation: \`${operationId}\``,
  `Changed file: \`${selectedPath}\``,
  `Before: \`${beforeSha}\``,
  `After: \`${afterSha}\``,
  "",
  "Boundary: pending recorder validation.",
  "Build: pending generated project build.",
  "",
]);

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
    allowedOperation: String(file.allowedOperation ?? "replace"),
    targetProject: String(file.targetProject ?? inferTargetProject("", targetPath)),
    projectRelativePath: String(file.projectRelativePath ?? inferProjectRelativePath("", targetPath)),
    sourceEvidenceReferences: file.sourceEvidenceReferences ?? [],
    slots: file.slots ?? [],
  };
}

function fileTargetFromAllowedOutput(file, projectName) {
  return {
    targetPath: file.targetPath,
    targetProject: file.targetProject || inferTargetProject(projectName, file.targetPath),
    projectRelativePath: file.projectRelativePath || inferProjectRelativePath(projectName, file.targetPath),
    ownership: file.ownership || "generated",
    allowedOperation: file.allowedOperation || "replace",
    visualShellOnly: file.visualShellOnly === true,
    planEntryId: file.planEntryId,
  };
}

function normalizeProtectedTargets(packageManifest, projectName) {
  const grouped = [
    ...(packageManifest.protectedFilesByProject?.server ?? []),
    ...(packageManifest.protectedFilesByProject?.wasm ?? []),
  ];
  const targets = grouped.length > 0
    ? grouped
    : (packageManifest.protectedFiles ?? []).map(path => ({
      targetPath: path,
      targetProject: inferTargetProject(projectName, path),
      projectRelativePath: inferProjectRelativePath(projectName, path),
      ownership: "protected",
    }));

  return targets.map(target => {
    const targetPath = normalizeProtectedTargetPath(target.targetPath);
    return {
    targetPath,
    targetProject: target.targetProject || inferTargetProject(projectName, targetPath),
    projectRelativePath: target.projectRelativePath || inferProjectRelativePath(projectName, targetPath),
    ownership: target.ownership || "protected",
    visualShellOnly: target.visualShellOnly === true,
  };
  }).sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
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

function normalizeProtectedTargetPath(path) {
  const normalized = String(path ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
  if (normalized.startsWith("protected-path:") && !normalized.includes("/") && !normalized.includes("\\")) {
    return normalized;
  }

  return normalizeTargetPath(path);
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

function inferTargetProject(projectName, targetPath) {
  const normalized = normalizeTargetPath(targetPath);
  return projectName && normalized.startsWith(`${projectName}.WASM/`) ? "wasm" : "server";
}

function inferProjectRelativePath(projectName, targetPath) {
  const normalized = normalizeTargetPath(targetPath);
  return projectName && normalized.startsWith(`${projectName}.WASM/`) ? normalized.slice(`${projectName}.WASM/`.length) : normalized;
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

function writeMarkdown(path, lines) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${lines.join("\n")}\n`, "utf8");
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
