#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, isAbsolute, join, resolve } from "node:path";

if (process.argv.includes("--help") || process.argv.includes("-h")) {
  console.log(`Usage: node record-agent-visual-writes.mjs --project-root <generated-project-root> [--from-checkpoint <path>] [--written-files <file[,file...]>]

Options:
  --project-root <path>             Generated storefront project root.
  --task-package <path>             Agent task package root. Defaults under project docs/storefront-analysis.
  --from-checkpoint <path>          Visual checkpoint JSON with pre/post source snapshots for auto-detection.
  --implementation-report <path>    Optional implementation report used to verify claimed changed files.
  --written-files <csv>             Optional hint/backcompat list of generated visual files changed by the agent.
  --closure-mode                    Fail hint-only mismatch instead of warning.
  --help, -h                        Show this help text.`);
  process.exit(0);
}

const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const taskPackageRoot = resolve(readArg("--task-package") ?? join(projectRoot, "docs/storefront-analysis/agent-task-package"));
const checkpointPath = readArg("--from-checkpoint") ? resolve(projectRoot, readArg("--from-checkpoint")) : null;
const implementationReportPath = readArg("--implementation-report")
  ? resolve(projectRoot, readArg("--implementation-report"))
  : join(projectRoot, "docs/storefront-analysis/visual-implementation-report.json");
const closureMode = hasArg("--closure-mode");
const writtenFiles = readListArg("--written-files").map(normalizeTargetPath);

if (writtenFiles.length === 0 && !checkpointPath) {
  fail("SFB-AGENT-WRITE-000", "Supply --from-checkpoint for auto-detection or --written-files for compatibility recording.");
}

const manifestPath = join(taskPackageRoot, "manifest.json");
if (!existsSync(manifestPath)) {
  fail("SFB-AGENT-WRITE-001", `Agent task package manifest is missing: ${manifestPath}`);
}

const packageManifest = readJson(manifestPath);
const allowedByPath = new Map((packageManifest.allowedOutputFiles ?? []).map(file => [normalizeTargetPath(file.targetPath), file]));
const detection = checkpointPath ? detectFromCheckpoint(checkpointPath, allowedByPath) : {
  mode: "hint-only",
  checkpointPath: null,
  detectedFiles: [],
  deletedFiles: [],
  unexpectedFiles: [],
  sourceTreeSnapshotScope: [],
};
const targetFiles = resolveTargetFiles(detection, writtenFiles);
const hintMismatch = buildHintMismatch(detection, writtenFiles);
const records = [];

for (const targetPath of targetFiles) {
  assertAllowedPath(targetPath, allowedByPath);
  const fullPath = resolve(projectRoot, targetPath);
  assertUnderProject(fullPath, targetPath);
  if (!existsSync(fullPath)) {
    fail("SFB-AGENT-WRITE-002", `Agent-written file does not exist: ${targetPath}`);
  }

  const allowed = allowedByPath.get(targetPath);
  const content = readFileSync(fullPath, "utf8");
  validateVisualContent(targetPath, content, allowed);
  records.push({
    filePath: targetPath,
    project: allowed.targetProject ?? inferTargetProject(packageManifest.projectName, targetPath),
    projectRelativePath: allowed.projectRelativePath ?? inferProjectRelativePath(packageManifest.projectName, targetPath),
    artifactRootRelativePath: allowed.artifactRootRelativePath ?? `${packageManifest.projectName}/${targetPath}`,
    detectionSource: describeDetectionSource(targetPath, detection, writtenFiles),
    sourcePlanEntryId: allowed.planEntryId,
    checksum: `sha256:${sha(normalizeText(content))}`,
    slots: allowed.slots ?? [],
    ownership: allowed.ownership,
    visualShellOnly: allowed.visualShellOnly === true,
  });
}

const output = join(projectRoot, "docs/storefront-analysis/agent-written-files.json");
const document = {
  schemaVersion: "1.0.0",
  artifactKind: "agent-written-files",
  artifactId: `agent-written-files.${packageManifest.projectName}`,
  detectionMode: detection.mode,
  checkpointPath: detection.checkpointPath,
  generationPlanHash: packageManifest.generationPlanHash,
  hintMismatch,
  hintFiles: writtenFiles,
  detectedFiles: detection.detectedFiles,
  deletedFiles: detection.deletedFiles,
  unexpectedFiles: detection.unexpectedFiles,
  files: records.sort((a, b) => a.filePath.localeCompare(b.filePath, "en")),
};

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${JSON.stringify(stableObject(document), null, 2)}\n`, "utf8");
appendAgentManifestSection(projectRoot, document.files);
console.log(`StorefrontBuilder recorded ${records.length} agent-written file(s).`);

function detectFromCheckpoint(path, allowedByPath) {
  if (!existsSync(path)) {
    fail("SFB-AGENT-WRITE-020", `Visual checkpoint does not exist: ${path}`);
  }

  const checkpoint = readJson(path);
  const scope = normalizeUnique(checkpoint.sourceTreeSnapshotScope ?? []);
  const explicitUnexpected = normalizeUnique(checkpoint.unexpectedFiles ?? []);
  if (explicitUnexpected.length > 0) {
    fail("SFB-AGENT-WRITE-021", `Visual checkpoint reports unexpected files outside closure scope: ${explicitUnexpected.join(", ")}`);
  }

  const pre = mapFileHashes(checkpoint.preEditFileHashes ?? []);
  const post = mapFileHashes(checkpoint.postEditFileHashes ?? []);
  const detected = new Set(normalizeUnique(checkpoint.changedFiles ?? []));
  const deleted = new Set();

  if (closureMode) {
    assertNoPlaceholderHashes(pre, post);
    assertCheckpointHashesMatchCurrentSource(scope, pre, post, detected);
  }

  for (const file of new Set([...scope, ...pre.keys(), ...post.keys()])) {
    const before = pre.get(file) ?? "missing";
    const after = post.get(file) ?? "missing";
    if (before !== after) {
      if (after === "missing") {
        deleted.add(file);
      } else {
        detected.add(file);
      }
    }
  }

  const unexpected = [...detected].filter(file => !allowedByPath.has(file));
  const protectedUnexpected = [...detected, ...deleted].filter(isProtectedPackagePath);
  if (unexpected.length > 0 || protectedUnexpected.length > 0) {
    fail("SFB-AGENT-WRITE-022", `Auto-detected files outside allowed generated visual outputs: ${normalizeUnique([...unexpected, ...protectedUnexpected]).join(", ")}`);
  }

  if (deleted.size > 0) {
    fail("SFB-AGENT-WRITE-023", `Auto-detection found deleted generated visual files, which cannot be recorded as valid closure writes: ${[...deleted].join(", ")}`);
  }

  assertImplementationReportClaims([...detected], pre, post);

  return {
    mode: "checkpoint-auto-detect",
    checkpointPath: relativeToProject(path),
    detectedFiles: normalizeUnique([...detected]),
    deletedFiles: normalizeUnique([...deleted]),
    unexpectedFiles: normalizeUnique([...unexpected, ...protectedUnexpected]),
    sourceTreeSnapshotScope: scope,
  };
}

function resolveTargetFiles(detection, hints) {
  if (detection.mode === "hint-only") {
    return normalizeUnique(hints);
  }

  const detected = detection.detectedFiles;
  if (detected.length === 0) {
    fail("SFB-AGENT-WRITE-024", "Auto-detection found no generated visual file changes.");
  }

  const omittedHints = detected.filter(file => hints.length > 0 && !hints.includes(file));
  if (omittedHints.length > 0) {
    fail("SFB-AGENT-WRITE-025", `--written-files omitted auto-detected changed files: ${omittedHints.join(", ")}`);
  }

  const unchangedHints = hints.filter(file => !detected.includes(file));
  if (unchangedHints.length > 0) {
    const message = `--written-files included unchanged or untracked files: ${unchangedHints.join(", ")}`;
    if (closureMode) {
      fail("SFB-AGENT-WRITE-026", message);
    }

    console.warn(`[SFB-AGENT-WRITE-WARN] ${message}`);
  }

  return detected;
}

function assertNoPlaceholderHashes(pre, post) {
  for (const [file, hash] of [...pre.entries(), ...post.entries()]) {
    if (/^sha256:phase4-11-|^sha256:(before|after|visual-plan|checklist|placeholder|fake|hash)$/i.test(hash)) {
      fail("SFB-AGENT-WRITE-033", `Visual checkpoint contains placeholder hash '${hash}' for ${file}. Closure mode requires current SHA-256 values.`);
    }
  }
}

function assertCheckpointHashesMatchCurrentSource(scope, pre, post, declaredChangedFiles) {
  for (const file of scope) {
    if (!post.has(file)) {
      fail("SFB-AGENT-WRITE-030", `Closure checkpoint sourceTreeSnapshotScope includes '${file}' but postEditFileHashes does not list it.`);
    }

    if (!pre.has(file)) {
      fail("SFB-AGENT-WRITE-030", `Closure checkpoint sourceTreeSnapshotScope includes '${file}' but preEditFileHashes does not list it.`);
    }
  }

  for (const file of post.keys()) {
    const fullPath = resolve(projectRoot, file);
    assertUnderProject(fullPath, file);
    if (!existsSync(fullPath)) {
      fail("SFB-AGENT-WRITE-029", `Checkpoint claims changed file '${file}' but the current generated file does not exist.`);
    }

    const currentHash = `sha256:${sha(normalizeText(readFileSync(fullPath, "utf8")))}`;
    const expectedPostHash = post.get(file);
    if (currentHash !== expectedPostHash) {
      fail("SFB-AGENT-WRITE-029", `Checkpoint post hash differs from current file content for ${file}. Expected ${expectedPostHash}, actual ${currentHash}.`);
    }

    const beforeHash = pre.get(file) ?? "missing";
    if (beforeHash !== currentHash && !declaredChangedFiles.has(file)) {
      fail("SFB-AGENT-WRITE-031", `Current file changed but checkpoint did not list it in changedFiles: ${file}`);
    }
  }
}

function assertImplementationReportClaims(detectedFiles, pre, post) {
  if (!existsSync(implementationReportPath)) {
    return;
  }

  const report = readJson(implementationReportPath);
  const claimed = normalizeUnique(report.changedFiles ?? []);
  if (detectedFiles.length === 0 && claimed.length > 0) {
    fail("SFB-AGENT-WRITE-027", `Implementation report claims changed files but checkpoint auto-detection found none: ${claimed.join(", ")}`);
  }

  const missingDetected = claimed.filter(file => !detectedFiles.includes(file));
  const unclaimedDetected = detectedFiles.filter(file => !claimed.includes(file));
  if (missingDetected.length > 0 || unclaimedDetected.length > 0) {
    fail("SFB-AGENT-WRITE-028", `Implementation report changedFiles differ from checkpoint changed files. Missing from checkpoint: ${missingDetected.join(", ") || "(none)"}. Missing from report: ${unclaimedDetected.join(", ") || "(none)"}.`);
  }

  for (const change of report.fileChanges ?? []) {
    const file = normalizeTargetPath(change.filePath);
    const checkpointBefore = pre.get(file) ?? "missing";
    const checkpointAfter = post.get(file) ?? "missing";
    const reportBefore = String(change.beforeSha256 ?? "missing");
    const reportAfter = String(change.afterSha256 ?? "missing");
    if (checkpointBefore !== reportBefore || checkpointAfter !== reportAfter) {
      fail("SFB-AGENT-WRITE-034", `Implementation report before/after hashes differ from checkpoint hashes for ${file}.`);
    }
  }
}

function buildHintMismatch(detection, hints) {
  if (detection.mode === "hint-only") {
    return {
      omittedChangedFiles: [],
      unchangedHintFiles: [],
    };
  }

  return {
    omittedChangedFiles: detection.detectedFiles.filter(file => hints.length > 0 && !hints.includes(file)),
    unchangedHintFiles: hints.filter(file => !detection.detectedFiles.includes(file)),
  };
}

function describeDetectionSource(targetPath, detection, hints) {
  if (detection.mode === "hint-only") {
    return "hint-only";
  }

  if (hints.includes(targetPath)) {
    return "auto-detected+hint-agreed";
  }

  return "auto-detected";
}

function assertAllowedPath(targetPath, allowedByPath) {
  if (isProtectedPackagePath(targetPath)) {
    fail("SFB-AGENT-WRITE-003", `Agent write targets a protected package or Starter zone: ${targetPath}`);
  }

  const allowed = allowedByPath.get(targetPath);
  if (!allowed) {
    fail("SFB-AGENT-WRITE-004", `Agent write is outside allowed generated-owned outputs: ${targetPath}`);
  }

  const inferredProject = inferTargetProject(packageManifest.projectName, targetPath);
  const inferredProjectRelativePath = inferProjectRelativePath(packageManifest.projectName, targetPath);
  if ((allowed.targetProject && allowed.targetProject !== inferredProject) ||
      (allowed.projectRelativePath && allowed.projectRelativePath !== inferredProjectRelativePath)) {
    fail("SFB-AGENT-WRITE-007", `Agent write crosses planned project boundary: ${targetPath}`);
  }

  if (targetPath === "appsettings.json" ||
      targetPath === "nuget.config" ||
      targetPath === "StorefrontPackageVersions.props" ||
      targetPath === "starter-generation.contract.yaml" ||
      targetPath.endsWith(".csproj") ||
      targetPath === "Program.cs" ||
      targetPath.endsWith("/Program.cs")) {
    fail("SFB-AGENT-WRITE-005", `Agent write targets project/server configuration: ${targetPath}`);
  }
}

function validateVisualContent(targetPath, content, allowed) {
  if (/^\s*@page\s+/m.test(content)) {
    fail("SFB-AGENT-WRITE-010", `Agent visual file must not declare routes: ${targetPath}`);
  }

  for (const token of [
    "HttpClient",
    "IHttpClientFactory",
    "fetch(",
    "XMLHttpRequest",
    "/api/storefront/stores/",
    "CommerceNodeBaseUrl",
    "MapGet(",
    "MapPost(",
    "MapPut(",
    "MapDelete(",
    "MapGroup(",
    "AddHttpClient",
    "AddStorefrontRuntime",
    "AddStorefrontPlatformRuntime",
    "StorefrontRuntimeOptions",
  ]) {
    if (content.includes(token)) {
      fail("SFB-AGENT-WRITE-011", `Agent visual file contains forbidden transport/server token '${token}': ${targetPath}`);
    }
  }

  for (const token of [
    "class Storefront",
    "record Storefront",
    "PlaceOrder",
    "CapturePayment",
    "ValidateCheckout",
    "ValidateCart",
    "ExpectedCartVersion",
    "ExpectedCheckoutVersion",
    "accessToken",
    "refreshToken",
    "rel=\"canonical\"",
  ]) {
    if (content.includes(token)) {
      fail("SFB-AGENT-WRITE-012", `Agent visual file contains forbidden business/auth/SEO token '${token}': ${targetPath}`);
    }
  }

  if ((allowed.slots ?? []).includes("product.purchase")) {
    for (const descriptor of ["data-storefront-product-purchase", "data-storefront-command=\"cart.add-line\""]) {
      if (!content.includes(descriptor)) {
        fail("SFB-AGENT-WRITE-013", `Product purchase visuals must preserve Presentation descriptor '${descriptor}': ${targetPath}`);
      }
    }
  }
}

function appendAgentManifestSection(projectRoot, records) {
  const manifestPath = join(projectRoot, "docs/storefront-analysis/generated-files.yaml");
  const existing = existsSync(manifestPath) ? readFileSync(manifestPath, "utf8").replace(/\r\n/g, "\n").replace(/\r/g, "\n") : "";
  const base = updateGeneratedFileManifestState(existing.replace(/\nagentWrittenFiles:\n[\s\S]*$/m, "").trimEnd(), records);
  const section = [
    "",
    "agentWrittenFiles:",
    ...records.flatMap(record => [
      `  - agentFilePath: ${record.filePath}`,
      `    project: ${record.project}`,
      `    projectRelativePath: ${record.projectRelativePath}`,
      `    artifactRootRelativePath: ${record.artifactRootRelativePath}`,
      `    sourcePlanEntryId: ${record.sourcePlanEntryId}`,
      `    checksum: ${record.checksum}`,
      `    ownership: ${record.ownership}`,
    ]),
    "",
  ].join("\n");
  mkdirSync(dirname(manifestPath), { recursive: true });
  writeFileSync(manifestPath, `${base}${section}`, "utf8");
}

function updateGeneratedFileManifestState(content, records) {
  let updated = content;
  for (const record of records) {
    updated = updateGeneratedFileManifestEntry(updated, record);
  }

  return updated;
}

function updateGeneratedFileManifestEntry(content, record) {
  const filePathLine = `  - filePath: ${record.filePath}`;
  const start = content.indexOf(filePathLine);
  if (start < 0) {
    return content;
  }

  const next = content.indexOf("\n  - filePath: ", start + filePathLine.length);
  const end = next < 0 ? content.length : next;
  const entry = content.slice(start, end);
  const updatedEntry = replaceYamlScalar(
    replaceYamlScalar(
      replaceYamlScalar(
        replaceYamlScalar(entry, "currentHash", record.checksum),
        "manualEditDetected",
        "true"),
      "conflictStatus",
      "agent-visual-edit"),
    "conflictReason",
    "Recorded by record-agent-visual-writes.mjs from a verified visual checkpoint.");

  return `${content.slice(0, start)}${updatedEntry}${content.slice(end)}`;
}

function replaceYamlScalar(entry, key, value) {
  const pattern = new RegExp(`^(\\s+${escapeRegex(key)}:\\s*).*$`, "m");
  if (!pattern.test(entry)) {
    return `${entry.replace(/\s*$/, "")}\n    ${key}: ${value}\n`;
  }

  return entry.replace(pattern, `$1${value}`);
}

function normalizeTargetPath(path) {
  const normalized = String(path ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
  if (!normalized || isAbsolute(normalized) || normalized.includes(":") || normalized.startsWith("../") || normalized.includes("/../")) {
    fail("SFB-AGENT-WRITE-006", `Unsafe agent write path: ${path}`);
  }

  return normalized;
}

function inferTargetProject(projectName, targetPath) {
  return normalizeTargetPath(targetPath).startsWith(`${projectName}.WASM/`) ? "wasm" : "server";
}

function inferProjectRelativePath(projectName, targetPath) {
  const normalized = normalizeTargetPath(targetPath);
  return normalized.startsWith(`${projectName}.WASM/`) ? normalized.slice(`${projectName}.WASM/`.length) : normalized;
}

function isProtectedPackagePath(path) {
  return /(^|\/)(BlazorShop\.Storefront\.Starter|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Client|BlazorShop\.Storefront\.Browser|BlazorShop\.Storefront\.Components)(\/|$)/.test(path);
}

function assertUnderProject(fullPath, targetPath) {
  const root = resolve(projectRoot);
  if (fullPath !== root && !fullPath.startsWith(`${root}\\`) && !fullPath.startsWith(`${root}/`)) {
    fail("SFB-AGENT-WRITE-006", `Agent write escapes generated project: ${targetPath}`);
  }
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function mapFileHashes(items) {
  const map = new Map();
  for (const item of items) {
    if (!item?.filePath) {
      continue;
    }

    map.set(normalizeTargetPath(item.filePath), String(item.sha256 ?? "missing"));
  }

  return map;
}

function normalizeUnique(items) {
  return [...new Set((items ?? []).map(normalizeTargetPath))].sort((a, b) => a.localeCompare(b, "en"));
}

function relativeToProject(path) {
  const root = resolve(projectRoot);
  const full = resolve(path);
  if (full === root) {
    return ".";
  }

  if (full.startsWith(`${root}\\`) || full.startsWith(`${root}/`)) {
    return full.slice(root.length + 1).replaceAll("\\", "/");
  }

  return full.replaceAll("\\", "/");
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

function normalizeText(value) {
  return value.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function fail(code, message) {
  throw new Error(`[${code}] ${message}`);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function hasArg(name) {
  return process.argv.includes(name);
}

function readListArg(name) {
  const value = readArg(name);
  if (!value) {
    return [];
  }

  return value.split(",").map(item => item.trim()).filter(Boolean);
}
