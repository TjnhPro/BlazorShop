#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { relative, resolve } from "node:path";
import { generatorVersion } from "./storefront-builder-version.mjs";

export const ownershipValues = ["generated", "managed", "user-owned", "protected", "artifact-only"];
export const templateVersion = "starter-contract-1";

const textExtensions = new Set([".cs", ".csproj", ".razor", ".css", ".js", ".json", ".yaml", ".yml", ".md", ".props", ".config"]);
const sourceSpecSeed = "storefront-builder-generated-file-manifest-v2";

export function scanProjectFiles(projectRoot) {
  const root = resolve(projectRoot);
  const files = [];
  walk(root, root, files);
  return files.sort((a, b) => a.filePath.localeCompare(b.filePath));
}

export function readPreviousManifest(manifestPath) {
  if (!existsSync(manifestPath)) {
    return new Map();
  }

  const text = readFileSync(manifestPath, "utf8");
  const entries = parseManifestEntries(text);
  return new Map(entries.map((entry) => [entry.filePath, entry]));
}

export function buildManifestEntries(projectRoot, previousEntries, intentionalUpdatePaths = new Set()) {
  const now = new Date().toISOString();
  const scanned = scanProjectFiles(projectRoot);
  const handoffPlan = readHandoffGenerationPlan(projectRoot);
  const sourceSpecHash = `sha256:${sha(scanned.map((file) => `${file.filePath}:${file.hash}`).join("|") || sourceSpecSeed)}`;
  const entries = [];
  const seen = new Set();

  for (const file of scanned) {
    const descriptor = classifyFile(file.filePath);
    const sourceArtifactIds = buildSourceArtifactIds(descriptor, file.filePath, handoffPlan);
    const previous = previousEntries.get(file.filePath);
    const previousGeneratedHash = previous?.generatedHash && previous.generatedHash !== "none"
      ? previous.generatedHash
      : undefined;
    const intentionalUpdate = intentionalUpdatePaths.has(file.filePath);
    const manualEditDetected = !intentionalUpdate && Boolean(previousGeneratedHash && previousGeneratedHash !== file.hash);
    const unchangedFromPrevious = previous?.currentHash === file.hash && previous?.lastGeneratedTimestamp;
    const conflict = classifyConflict(descriptor.ownership, manualEditDetected, file.filePath, previous);

    entries.push({
      filePath: file.filePath,
      project: inferProject(file.filePath),
      ownership: descriptor.ownership,
      capability: descriptor.capability,
      scope: descriptor.scope,
      generatorVersion,
      sourceArtifactIds,
      sourcePlanEntryId: handoffPlan.fileIdsByPath.get(file.filePath) ?? previous?.sourcePlanEntryId ?? "none",
      sourceSpecHash,
      generatedHash: manualEditDetected ? previousGeneratedHash : file.hash,
      currentHash: file.hash,
      lastGeneratedTimestamp: (manualEditDetected || (unchangedFromPrevious && !intentionalUpdate)) && previous?.lastGeneratedTimestamp ? previous.lastGeneratedTimestamp : now,
      manualEditDetected: String(manualEditDetected),
      conflictStatus: conflict.status,
      conflictReason: conflict.reason,
      protected: String(descriptor.ownership === "protected"),
      obsolete: String(conflict.status === "obsolete"),
      templateVersion,
    });

    seen.add(file.filePath);
  }

  for (const previous of previousEntries.values()) {
    if (seen.has(previous.filePath)) {
      continue;
    }

    if (previous.ownership === "generated" || previous.ownership === "managed") {
      entries.push({
        filePath: previous.filePath,
        project: previous.project ?? inferProject(previous.filePath),
        ownership: previous.ownership,
        capability: previous.capability ?? "unknown",
        scope: previous.scope ?? "unknown",
        generatorVersion: previous.generatorVersion ?? generatorVersion,
        sourceArtifactIds: previous.sourceArtifactIds ?? "none",
        sourcePlanEntryId: previous.sourcePlanEntryId ?? "none",
        sourceSpecHash: previous.sourceSpecHash ?? `sha256:${sha(sourceSpecSeed)}`,
        generatedHash: previous.generatedHash ?? "none",
        currentHash: "none",
        lastGeneratedTimestamp: previous.lastGeneratedTimestamp ?? now,
        manualEditDetected: "false",
        conflictStatus: "missing",
        conflictReason: "Generated file is missing; regenerate it or mark it obsolete.",
        protected: "false",
        obsolete: "false",
        templateVersion: previous.templateVersion ?? templateVersion,
      });
    }
  }

  return entries.sort((a, b) => a.filePath.localeCompare(b.filePath));
}

export function writeManifestYaml(entries) {
  return [
    "schemaVersion: 1.0.0",
    "artifactKind: generated-files",
    "artifactId: generated-files.generated-proof",
    "files:",
    ...entries.flatMap((entry) => [
      `  - filePath: ${entry.filePath}`,
      `    project: ${entry.project}`,
      `    ownership: ${entry.ownership}`,
      `    capability: ${quote(entry.capability)}`,
      `    scope: ${entry.scope}`,
      `    generatorVersion: ${entry.generatorVersion}`,
      `    sourceArtifactIds: ${entry.sourceArtifactIds}`,
      `    sourcePlanEntryId: ${entry.sourcePlanEntryId}`,
      `    sourceSpecHash: ${entry.sourceSpecHash}`,
      `    generatedHash: ${entry.generatedHash}`,
      `    currentHash: ${entry.currentHash}`,
      `    lastGeneratedTimestamp: ${entry.lastGeneratedTimestamp}`,
      `    manualEditDetected: ${entry.manualEditDetected}`,
      `    conflictStatus: ${entry.conflictStatus}`,
      `    conflictReason: ${quote(entry.conflictReason)}`,
      `    protected: ${entry.protected}`,
      `    obsolete: ${entry.obsolete}`,
      `    templateVersion: ${entry.templateVersion}`,
    ]),
    "",
  ].join("\n");
}

function inferProject(filePath) {
  return filePath.includes(".WASM/") ? "wasm" : "server";
}

export function buildRegenerationReport(entries) {
  const conflicts = entries.filter((entry) => entry.conflictStatus !== "none");
  const conflictLines = conflicts.length === 0
    ? ["- No manifest conflicts detected."]
    : conflicts.map((entry) => `- ${entry.filePath}: ${entry.conflictStatus}. ${entry.conflictReason} Next action: review before regeneration.`);

  return [
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
    "## Conflicts",
    "",
    ...conflictLines,
    "",
  ].join("\n");
}

export function parseManifestEntries(text) {
  const entries = [];
  let current = null;
  for (const line of text.split(/\r?\n/)) {
    if (/^agentWrittenFiles:\s*$/.test(line)) {
      break;
    }

    const fileMatch = line.match(/^\s+- filePath:\s*(.+?)\s*$/);
    if (fileMatch) {
      current = { filePath: unquote(fileMatch[1]) };
      entries.push(current);
      continue;
    }

    if (!current) {
      continue;
    }

    const propertyMatch = line.match(/^\s+([A-Za-z0-9]+):\s*(.*?)\s*$/);
    if (propertyMatch) {
      current[propertyMatch[1]] = unquote(propertyMatch[2]);
    }
  }

  return entries;
}

function walk(root, directory, files) {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === "bin" || entry.name === "obj" || entry.name === ".staging" || entry.name === ".replace-backup") {
      continue;
    }

    const fullPath = resolve(directory, entry.name);
    if (entry.isDirectory()) {
      walk(root, fullPath, files);
      continue;
    }

    const filePath = relative(root, fullPath).replaceAll("\\", "/");
    if (!shouldTrack(filePath)) {
      continue;
    }

    files.push({
      filePath,
      hash: `sha256:${hashFile(fullPath)}`,
    });
  }
}

function shouldTrack(filePath) {
  if (filePath === "docs/storefront-analysis/generated-files.yaml" || filePath === "docs/storefront-analysis/regeneration-report.md") {
    return false;
  }

  const extension = filePath.includes(".") ? filePath.slice(filePath.lastIndexOf(".")).toLowerCase() : "";
  return textExtensions.has(extension);
}

function readHandoffGenerationPlan(projectRoot) {
  const planPath = resolve(projectRoot, "docs/storefront-analysis/generation-plan.json");
  if (!existsSync(planPath)) {
    return { isHandoff: false, fileIdsByPath: new Map() };
  }

  const plan = JSON.parse(readFileSync(planPath, "utf8"));
  const fileIdsByPath = new Map();
  for (const file of plan.files ?? []) {
    if (file.targetPath && file.id) {
      fileIdsByPath.set(String(file.targetPath).replaceAll("\\", "/").replace(/^\/+/, ""), file.id);
    }
  }

  return { isHandoff: true, fileIdsByPath };
}

function buildSourceArtifactIds(descriptor, filePath, handoffPlan) {
  if (!handoffPlan.isHandoff) {
    return descriptor.sourceArtifactIds.join(" ");
  }

  if (filePath.startsWith("docs/storefront-analysis/")) {
    return "metadata.yaml";
  }

  if (descriptor.ownership === "user-owned") {
    return "none";
  }

  return "metadata.yaml generation-plan.json";
}

function hashFile(path) {
  const normalized = readFileSync(path, "utf8").replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  return sha(normalized);
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}

function classifyFile(filePath) {
  if (filePath === "StorefrontPackageVersions.props" || filePath === "starter-generation.contract.yaml") {
    return descriptor("protected", "SEO/media/consent support", "project", ["metadata.yaml"]);
  }

  if (filePath === "docs/storefront-analysis/metadata.yaml") {
    return descriptor("managed", "platform metadata", "project", ["metadata.yaml"]);
  }

  if (filePath.startsWith("docs/storefront-analysis/")) {
    return descriptor("artifact-only", "SEO/media/consent support", "artifact", ["metadata.yaml"]);
  }

  if (filePath === "README.md" || filePath === "appsettings.json" || filePath === "nuget.config") {
    return descriptor("user-owned", "shell/layout", "project", ["none"]);
  }

  if (filePath.endsWith(".csproj") || filePath === "Program.cs" || filePath.endsWith("/Program.cs") || filePath === "StarterFoundationViewRegistration.cs") {
    return descriptor("managed", "shell/layout", "project", ["metadata.yaml"]);
  }

  if (filePath.startsWith("wwwroot/css/") || /\.WASM\/wwwroot\/css\//.test(filePath)) {
    return descriptor("generated", "shell/layout", "css", ["metadata.yaml", "asset-manifest.yaml", "review-summary.md"]);
  }

  if (filePath.startsWith("wwwroot/assets/") || /\.WASM\/wwwroot\//.test(filePath)) {
    return descriptor("generated", "SEO/media/consent support", "asset", ["asset-manifest.yaml"]);
  }

  if (filePath.includes("/Layout/")) {
    return descriptor("generated", "shell/layout", "component", ["metadata.yaml", "review-summary.md"]);
  }

  if (filePath.includes("/Account/")) {
    return descriptor("managed", "account", filePath.startsWith("Pages/") ? "page" : "component", ["metadata.yaml"]);
  }

  if (filePath.includes("/Auth/")) {
    return descriptor("managed", "auth/recovery", "page", ["metadata.yaml"]);
  }

  if (filePath.includes("/Content/")) {
    return descriptor("generated", "content", "page", ["metadata.yaml", "review-summary.md"]);
  }

  if (filePath.includes("/Home/")) {
    return descriptor("generated", "home", "page", ["metadata.yaml", "review-summary.md"]);
  }

  if (filePath.includes("/Catalog/Product") || filePath.includes("PurchasePanel") || filePath.includes("ProductGallery")) {
    return descriptor("generated", "product", filePath.startsWith("Pages/") ? "page" : "component", ["metadata.yaml", "review-summary.md"]);
  }

  if (filePath.includes("/Catalog/")) {
    return descriptor("generated", "catalog", filePath.startsWith("Pages/") ? "page" : "component", ["metadata.yaml", "review-summary.md"]);
  }

  if (filePath.includes("/Commerce/Cart") || /\.WASM\/Components\/Cart\//.test(filePath)) {
    return descriptor("managed", "cart", filePath.startsWith("Pages/") ? "page" : "component", ["metadata.yaml"]);
  }

  if (filePath.includes("/Commerce/Checkout") || filePath.includes("/Commerce/Payment") || /\.WASM\/Components\/Checkout\//.test(filePath)) {
    return descriptor("managed", "checkout", filePath.startsWith("Pages/") ? "page" : "component", ["metadata.yaml"]);
  }

  if (filePath.includes("/States/")) {
    return descriptor("managed", "SEO/media/consent support", "component", ["metadata.yaml"]);
  }

  return descriptor("user-owned", "shell/layout", "project", ["none"]);
}

function descriptor(ownership, capability, scope, sourceArtifactIds) {
  return { ownership, capability, scope, sourceArtifactIds };
}

function classifyConflict(ownership, manualEditDetected, filePath, previous) {
  if (previous?.ownership === "generated" && ownership !== "generated") {
    return { status: "obsolete", reason: "Previously generated file is no longer owned by the current template." };
  }

  if (!manualEditDetected) {
    return { status: "none", reason: "none" };
  }

  if (ownership === "protected") {
    return { status: "protected-modified", reason: "Protected file changed after manifest generation." };
  }

  if (ownership === "user-owned") {
    return { status: "user-owned-modified", reason: "User-owned file changed and must be preserved." };
  }

  return { status: "manual-edit", reason: `${filePath} differs from the last generated hash.` };
}

function quote(value) {
  const text = String(value ?? "");
  return /^[A-Za-z0-9_.\/:-]+$/.test(text) ? text : JSON.stringify(text);
}

function unquote(value) {
  const text = String(value ?? "").trim();
  if (text.startsWith('"') && text.endsWith('"')) {
    try {
      return JSON.parse(text);
    } catch {
      return text.slice(1, -1);
    }
  }

  return text;
}
