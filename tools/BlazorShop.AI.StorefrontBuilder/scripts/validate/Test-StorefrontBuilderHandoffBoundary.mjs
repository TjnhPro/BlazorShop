#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { extname, join, relative, resolve } from "node:path";
import { parseManifestEntries } from "../generate/generated-file-manifest.mjs";

const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const name = readArg("--name");
const analysisRoot = join(projectRoot, "docs", "storefront-analysis");
const generationPlanPath = join(analysisRoot, "generation-plan.json");

if (!existsSync(generationPlanPath)) {
  console.log(`StorefrontBuilder handoff boundary validation skipped for non-handoff project: ${projectRoot}`);
  process.exit(0);
}

const metadataPath = join(analysisRoot, "metadata.yaml");
const generatedManifestPath = join(analysisRoot, "generated-files.yaml");
const agentPackageManifestPath = join(analysisRoot, "agent-task-package", "manifest.json");
const agentWrittenFilesPath = join(analysisRoot, "agent-written-files.json");
const planText = readFileSync(generationPlanPath, "utf8");
const plan = JSON.parse(planText);
const metadata = readFile(metadataPath);
const generatedManifest = readFile(generatedManifestPath);
const manifestEntries = parseManifestEntries(generatedManifest);
const manifestByPath = new Map(manifestEntries.map(entry => [normalizePath(entry.filePath), entry]));
const planFiles = (plan.files ?? []).map(file => ({
  ...file,
  targetPath: normalizePath(file.targetPath),
}));
const plannedByPath = new Map(planFiles.map(file => [file.targetPath, file]));

validateLineageArtifacts();
validateMetadataHashes();
validatePackageReferences();
validateGeneratedManifestPlanEntries();
validateAgentPackage();
validateTextBoundaries();
validateManifestFileState();

console.log(`StorefrontBuilder handoff boundary validation passed for ${projectRoot}.`);

function validateLineageArtifacts() {
  for (const artifact of [
    "metadata.yaml",
    "generation-plan.json",
    "generation-plan.yaml",
    "handoff-generation-summary.md",
    "handoff-placeholders.json",
    "generated-files.yaml",
    "regeneration-report.md",
    "agent-task-package/manifest.json",
  ]) {
    assertExists(join(analysisRoot, artifact), "SFB-HANDOFF-BOUNDARY-001", `Handoff lineage artifact is missing: docs/storefront-analysis/${artifact}`);
  }

  assertMetadataContains("generationMode: handoff-project-skeleton", "SFB-HANDOFF-BOUNDARY-002");
  assertMetadataContains("handoffGeneration:", "SFB-HANDOFF-BOUNDARY-002");
}

function validateMetadataHashes() {
  const planSha256 = metadataScalar("planSha256");
  const sourceHandoffPackageHash = metadataScalar("sourceHandoffPackageHash");
  const sourceHandoffReadinessHash = metadataScalar("sourceHandoffReadinessHash");
  const sourceStarterContractHash = metadataScalar("sourceStarterContractHash");

  if (planSha256 !== shaFile(generationPlanPath)) {
    fail("SFB-HANDOFF-BOUNDARY-010", "metadata.yaml handoffGeneration.planSha256 does not match docs/storefront-analysis/generation-plan.json.");
  }

  if (sourceHandoffPackageHash !== plan.sourceHandoffPackageHash) {
    fail("SFB-HANDOFF-BOUNDARY-011", "metadata.yaml sourceHandoffPackageHash does not match generation-plan.json.");
  }

  if (sourceHandoffReadinessHash !== plan.sourceHandoffReadinessHash) {
    fail("SFB-HANDOFF-BOUNDARY-012", "metadata.yaml sourceHandoffReadinessHash does not match generation-plan.json.");
  }

  if (sourceStarterContractHash !== plan.sourceStarterContractHash) {
    fail("SFB-HANDOFF-BOUNDARY-013", "metadata.yaml sourceStarterContractHash does not match generation-plan.json.");
  }
}

function validatePackageReferences() {
  const projectName = name ?? metadataScalar("projectName");
  const projectPath = join(projectRoot, `${projectName}.csproj`);
  const project = readFile(projectPath);

  for (const required of ["BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components"]) {
    if (!project.includes(`PackageReference Include="${required}"`)) {
      fail("SFB-HANDOFF-BOUNDARY-020", `Generated handoff project must consume ${required} as a package reference.`);
    }
  }

  for (const forbidden of ["BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client"]) {
    if (project.includes(`PackageReference Include="${forbidden}"`)) {
      fail("SFB-HANDOFF-BOUNDARY-021", `Generated handoff project must not direct-reference ${forbidden}.`);
    }
  }
}

function validateGeneratedManifestPlanEntries() {
  for (const planFile of planFiles) {
    if (!isGeneratedHandoffOutput(planFile)) {
      continue;
    }

    const entry = manifestByPath.get(planFile.targetPath);
    if (!entry) {
      fail("SFB-HANDOFF-BOUNDARY-030", `generated-files.yaml is missing planned handoff file: ${planFile.targetPath}`);
    }

    if (entry.sourcePlanEntryId !== planFile.id) {
      fail("SFB-HANDOFF-BOUNDARY-031", `generated-files.yaml entry ${planFile.targetPath} must record sourcePlanEntryId ${planFile.id}.`);
    }
  }
}

function validateAgentPackage() {
  const agentManifest = readJson(agentPackageManifestPath);
  const expectedPlanHash = `sha256:${sha(normalizeText(planText))}`;
  if (agentManifest.generationPlanHash !== expectedPlanHash) {
    fail("SFB-HANDOFF-BOUNDARY-040", "agent-task-package manifest generationPlanHash does not match generation-plan.json.");
  }

  const allowed = new Map((agentManifest.allowedOutputFiles ?? []).map(file => [normalizePath(file.targetPath), file.planEntryId]));
  for (const [targetPath, planEntryId] of allowed) {
    const planned = plannedByPath.get(targetPath);
    if (!planned || planned.id !== planEntryId || !isGeneratedHandoffOutput(planned)) {
      fail("SFB-HANDOFF-BOUNDARY-041", `agent-task-package allows an output outside the generation plan: ${targetPath}`);
    }
  }

  if (existsSync(agentWrittenFilesPath)) {
    const written = readJson(agentWrittenFilesPath);
    for (const record of written.files ?? []) {
      const targetPath = normalizePath(record.filePath);
      const planned = plannedByPath.get(targetPath);
      if (!planned || record.sourcePlanEntryId !== planned.id) {
        fail("SFB-HANDOFF-BOUNDARY-042", `agent-written-files.json has an invalid source plan entry for ${targetPath}.`);
      }

      const current = `sha256:${sha(normalizeText(readFile(join(projectRoot, targetPath))))}`;
      if (record.checksum !== current) {
        fail("SFB-HANDOFF-BOUNDARY-043", `agent-written-files.json checksum is stale for ${targetPath}.`);
      }
    }
  }
}

function validateTextBoundaries() {
  const forbiddenRawEvidence = [
    "captures/",
    "analysis/pages/",
    "analysis/resolved/",
    "presentation-catalog/",
    "review/",
    "reports/",
  ];
  const forbiddenProjectTokens = [
    "BlazorShop.Storefront.V2",
    "BlazorShop.Web.SharedV2",
    "Web.SharedV2",
    "BlazorShop.Application",
    "BlazorShop.Domain",
    "BlazorShop.Infrastructure",
    "BlazorShop.CommerceNode.API",
    "BlazorShop.ControlPlane.API",
    "BlazorShop.ControlPlane.Web",
  ];
  const browserTransportTokens = [
    "HttpClient",
    "IHttpClientFactory",
    "fetch(",
    "XMLHttpRequest",
    "/api/storefront/stores/",
    "CommerceNodeBaseUrl",
  ];

  for (const file of textFiles(projectRoot)) {
    const relativePath = normalizePath(relative(projectRoot, file));
    const content = readFile(file);

    for (const marker of [...forbiddenRawEvidence, ...forbiddenProjectTokens]) {
      if (content.includes(marker)) {
        fail("SFB-HANDOFF-BOUNDARY-050", `Generated handoff project contains forbidden source/reference token '${marker}' in ${relativePath}.`);
      }
    }

    if (extname(file).toLowerCase() === ".razor" && /^\s*@page\s+/m.test(content)) {
      fail("SFB-HANDOFF-BOUNDARY-051", `Generated handoff visual file must not declare @page routes: ${relativePath}`);
    }

    if (isBrowserSource(relativePath)) {
      for (const token of browserTransportTokens) {
        if (content.includes(token)) {
          fail("SFB-HANDOFF-BOUNDARY-052", `Generated browser source must not call Commerce Node or own transport: ${relativePath} contains ${token}`);
        }
      }
    }
  }
}

function validateManifestFileState() {
  const seen = new Set();
  for (const entry of manifestEntries) {
    const filePath = normalizePath(entry.filePath);
    seen.add(filePath);
    const fullPath = join(projectRoot, filePath);
    const exists = existsSync(fullPath);

    if (!exists) {
      if (entry.currentHash !== "none" || !["missing", "obsolete"].includes(entry.conflictStatus)) {
        fail("SFB-HANDOFF-BOUNDARY-060", `Missing generated file is not reported as missing/obsolete in generated-files.yaml: ${filePath}`);
      }

      continue;
    }

    const currentHash = `sha256:${sha(normalizeText(readFile(fullPath)))}`;
    if (entry.currentHash !== currentHash && entry.manualEditDetected !== "true") {
      fail("SFB-HANDOFF-BOUNDARY-061", `generated-files.yaml does not report the current file hash/manual edit for ${filePath}.`);
    }

    if ((entry.ownership === "protected" || entry.protected === "true") && entry.generatedHash !== currentHash) {
      fail("SFB-HANDOFF-BOUNDARY-062", `Protected generated file changed outside an approved foundation update: ${filePath}`);
    }

    if (entry.manualEditDetected === "true" && entry.conflictStatus === "none") {
      fail("SFB-HANDOFF-BOUNDARY-063", `Manual edit is not visible as a manifest conflict: ${filePath}`);
    }

    if (["missing", "obsolete"].includes(entry.conflictStatus) && (!entry.conflictReason || entry.conflictReason === "none")) {
      fail("SFB-HANDOFF-BOUNDARY-064", `Obsolete or missing generated file lacks a conflict reason: ${filePath}`);
    }
  }

  for (const planFile of planFiles) {
    if (isGeneratedHandoffOutput(planFile) && existsSync(join(projectRoot, planFile.targetPath)) && !seen.has(planFile.targetPath)) {
      fail("SFB-HANDOFF-BOUNDARY-065", `Existing handoff-generated file is missing from generated-files.yaml: ${planFile.targetPath}`);
    }
  }
}

function isGeneratedHandoffOutput(planFile) {
  return ["create", "replace", "patch"].includes(planFile.allowedOperation ?? planFile.action)
    && (planFile.ownership === "generated" || planFile.visualShellOnly === true);
}

function textFiles(root) {
  const files = [];
  const stack = [root];
  const extensions = new Set([".cs", ".csproj", ".razor", ".css", ".js", ".mjs", ".ts", ".json", ".yaml", ".yml", ".md", ".props", ".config"]);
  while (stack.length > 0) {
    const current = stack.pop();
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      if (["bin", "obj", ".staging", ".replace-backup"].includes(entry.name)) {
        continue;
      }

      const fullPath = join(current, entry.name);
      if (entry.isDirectory()) {
        stack.push(fullPath);
        continue;
      }

      if (extensions.has(extname(entry.name).toLowerCase())) {
        files.push(fullPath);
      }
    }
  }

  return files;
}

function isBrowserSource(relativePath) {
  return /\.(razor|js|mjs|ts)$/i.test(relativePath) && !relativePath.startsWith("docs/storefront-analysis/");
}

function metadataScalar(key) {
  const match = metadata.match(new RegExp(`^\\s*${escapeRegex(key)}:\\s*(\\S+)\\s*$`, "m"));
  if (!match) {
    fail("SFB-HANDOFF-BOUNDARY-003", `metadata.yaml is missing '${key}'.`);
  }

  return match[1];
}

function assertMetadataContains(text, code) {
  if (!metadata.includes(text)) {
    fail(code, `metadata.yaml is missing '${text}'.`);
  }
}

function assertExists(path, code, message) {
  if (!existsSync(path)) {
    fail(code, message);
  }
}

function readJson(path) {
  return JSON.parse(readFile(path));
}

function readFile(path) {
  if (!existsSync(path)) {
    fail("SFB-HANDOFF-BOUNDARY-000", `Required handoff validation file is missing: ${path}`);
  }

  return readFileSync(path, "utf8");
}

function normalizePath(value) {
  return String(value ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
}

function normalizeText(value) {
  return value.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}

function shaFile(path) {
  return createHash("sha256").update(readFileSync(path)).digest("hex");
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
